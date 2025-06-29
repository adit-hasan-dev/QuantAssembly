using Markdig;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using QuantAssembly.Analyst.Service;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.analystFinalOutput))]
    public class PublishStep : IPipelineStep<AnalystContext>
    {
        private const string reportEmailSubject = "QuantAssembly Analyst Options Report {0}";
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var config = baseConfig as Models.Config;
            logger.LogInfo($"[{nameof(PublishStep)}] Publishing final output");

            // Render final output to HTML
            logger.LogInfo($"[{nameof(PublishStep)}] Rendering final output to HTML");
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UsePipeTables()
                .UseTaskLists()
                .UseFigures()
                .Build();
            string htmlBody = $@"
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; color: #333; background: #f9f9f9; padding: 20px; }}
        h1, h2, h3 {{ color: #007acc; }}
        pre {{ background: #f4f4f4; padding: 10px; border-radius: 5px; font-family: monospace; }}
        blockquote {{ border-left: 4px solid #ccc; padding-left: 10px; color: #555; }}
        code {{ background: #eee; padding: 2px 5px; border-radius: 3px; font-family: monospace; }}
        table {{ border-collapse: collapse; width: 100%; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; }}
        th {{ background: #f2f2f2; }}
        ul, ol {{ padding-left: 20px; }}
    </style>
</head>
<body>
    {Markdown.ToHtml(context.composedFinalOutput, pipeline)}
</body>
</html>";
            
            // Save to disk
            string fileName = $"{config!.OutputFilePath}/OptionsReport_{DateTime.UtcNow:yyyy-MM-ddTHH}.html";
            File.WriteAllText(fileName, htmlBody);
            logger.LogInfo($"[{nameof(PublishStep)}] Successfully saved final output to {fileName}");

            // Publish to email
            var emailAddress = config.emailPublishConfig.SourceEmailAddress;
            var emailService = new EmailService(config.emailPublishConfig.SmtpServer, config.emailPublishConfig.SmtpPort, emailAddress, config.emailPublishConfig.AppPassword, logger);

            logger.LogInfo($"[{nameof(PublishStep)}] Sending email with final output to {emailAddress}");
            string emailSubject = string.Format(reportEmailSubject, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            await emailService.SendEmailAsync(config.emailPublishConfig.DestinationEmailAddress, emailSubject, htmlBody);
            logger.LogInfo($"[{nameof(PublishStep)}] Successfully published final output to {emailAddress}");
        }
    }
}