using Markdig;
using Microsoft.Extensions.DependencyInjection;
using QuantAssembly.Analyst.Service;
using QuantAssembly.Common.Config;
using QuantAssembly.Common.Logging;
using QuantAssembly.Common.Pipeline;

namespace QuantAssembly.Analyst
{
    [PipelineStep]
    [PipelineStepInput(nameof(AnalystContext.composedOutput))]
    public class PublishStep : IPipelineStep<AnalystContext>
    {
        private const string reportEmailSubject = "QuantAssembly Analyst Options Report {0}";
        public async Task Execute(AnalystContext context, ServiceProvider serviceProvider, BaseConfig baseConfig)
        {
            var logger = serviceProvider.GetRequiredService<ILogger>();
            logger.LogInfo($"[{nameof(PublishStep)}] Publishing final output");
            var config = baseConfig as Models.Config;
            var emailAddress = config.emailPublishConfig.EmailAddress;
            var emailService = new EmailService(config.emailPublishConfig.SmtpServer, config.emailPublishConfig.SmtpPort, emailAddress, config.emailPublishConfig.AppPassword, logger);

            logger.LogInfo($"[{nameof(PublishStep)}] Sending email with final output to {emailAddress}");
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
    {Markdown.ToHtml(context.composedOutput, pipeline)}
</body>
</html>";

            string emailSubject = string.Format(reportEmailSubject, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
            await emailService.SendEmailAsync(emailAddress, emailSubject, htmlBody);
            logger.LogInfo($"[{nameof(PublishStep)}] Successfully published final output to {emailAddress}");
        }
    }
}