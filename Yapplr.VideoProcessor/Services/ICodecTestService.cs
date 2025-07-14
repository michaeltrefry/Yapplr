namespace Yapplr.VideoProcessor.Services;

public interface ICodecTestService
{
    Task<CodecTestResult> RunCodecTestsAsync();
}