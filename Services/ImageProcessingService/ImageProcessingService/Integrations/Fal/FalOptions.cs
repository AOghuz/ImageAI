namespace ImageProcessingService.Integrations.Fal;

public sealed class FalOptions
{
    public string? ApiKey { get; set; }     // Dev: User Secrets; Prod: ENV FAL_KEY
    public string? BaseUrl { get; set; }    // https://queue.fal.run
    public string? ModelPath { get; set; }  // fal-ai/nano-banana/edit
    public int PollIntervalSeconds { get; set; } = 2;
    public int PollTimeoutSeconds { get; set; } = 120;
}
