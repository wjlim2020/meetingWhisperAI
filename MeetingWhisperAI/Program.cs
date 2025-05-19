using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Diagnostics;
using System.Text;
using NAudio.Wave;

partial class Program
{
    static string FindAudioFile(string fileName)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = new UTF8Encoding(false);

        if (File.Exists(fileName)) return Path.GetFullPath(fileName);

        var projectDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        var projectPath = Path.Combine(projectDir, fileName);
        return File.Exists(projectPath) ? projectPath : null;
    }

    static string ConvertMp3ToWav(string mp3Path)
    {
        var wavPath = Path.ChangeExtension(mp3Path, ".wav");
        Console.WriteLine("正在轉換 MP3 為 WAV 格式...");

        using var mp3Reader = new Mp3FileReader(mp3Path);
        var outFormat = new WaveFormat(16000, 16, 1);
        using var resampler = new MediaFoundationResampler(mp3Reader, outFormat)
        {
            ResamplerQuality = 60
        };

        WaveFileWriter.CreateWaveFile(wavPath, resampler);
        Console.WriteLine($"WAV 儲存於：{wavPath}");
        return wavPath;
    }

    static string RunWhisperCpp(string wavPath, string modelPath)
    {
        var workingDir = AppDomain.CurrentDomain.BaseDirectory;
        var transcriptPath = wavPath + ".txt";

        var psi = new ProcessStartInfo
        {
            FileName = "whisper-cli.exe",
            Arguments = $"-f \"{wavPath}\" -m \"{modelPath}\" --language zh --beam-size 5 --best-of 5 --output-txt --print-progress",
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        while (!process.StandardOutput.EndOfStream)
        {
            string? line = process.StandardOutput.ReadLine();
            Console.WriteLine(line);
        }

        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(stderr))
        {
            Console.WriteLine("stderr: " + stderr);
        }

        if (process.ExitCode != 0)
        {
            Console.WriteLine($"Whisper CLI 執行錯誤，代碼：{process.ExitCode}");
            return null;
        }

        if (!File.Exists(transcriptPath))
        {
            Console.WriteLine($"未找到轉譯檔案：{transcriptPath}");
            return null;
        }

        return transcriptPath;
    }

    public static async Task Main(string[] args)
    {
        try
        {
            var builder = Kernel.CreateBuilder();
            #pragma warning disable SKEXP0070
            builder.AddOllamaChatCompletion("qwen2.5:7b", new Uri("http://localhost:11434"));
            var kernel = builder.Build();

            const string audioFileName = "test.mp3";
            const string modelFile = "ggml-large-v3.bin";

            Console.WriteLine($"尋找音訊檔案：{audioFileName}");
            var audioPath = FindAudioFile(audioFileName);
            if (audioPath == null)
            {
                Console.WriteLine("找不到音訊檔案。請確認路徑是否正確。");
                return;
            }

            Console.WriteLine($"找到音訊檔案：{audioPath}");
            var wavPath = ConvertMp3ToWav(audioPath);

            Console.WriteLine("開始語音轉文字...");
            var transcriptPath = RunWhisperCpp(wavPath, modelFile);
            if (transcriptPath == null) return;

            var fullTranscript = await File.ReadAllTextAsync(transcriptPath, Encoding.UTF8);
            Console.WriteLine("\n轉譯內容：");
            Console.WriteLine(fullTranscript);

            Console.WriteLine("\n使用 AI 分析會議內容...");
            var prompt = @$"請分析以下會議記錄內容，並以條列方式總結其重點：\n{fullTranscript}";

            var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            var response = await chatCompletion.GetChatMessageContentAsync(chatHistory);
            Console.WriteLine("\nAI 分析結果：");
            Console.WriteLine(response.Content);

            try
            {
                File.Delete(wavPath);
                File.Delete(transcriptPath);
                Console.WriteLine("\n臨時檔案已刪除。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n刪除臨時檔案失敗：{ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"發生錯誤：{ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"↳內部錯誤：{ex.InnerException.Message}");
        }
    }
}
