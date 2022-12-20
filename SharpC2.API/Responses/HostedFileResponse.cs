﻿namespace SharpC2.API.Responses;

public sealed class HostedFileResponse
{
    public string Id { get; set; }
    public string Handler { get; set; }
    public string Uri { get; set; }
    public string Filename { get; set; }
    public long Size { get; set; }
}