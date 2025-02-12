// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class RabbitMQDelayedPluginExtensions
{
    private const string _registry = "docker.io";
    private const string _image = "library/rabbitmq";
    private const string _delayedPluginTag = "-delayed";

    /// <summary>
    /// Configures the RabbitMQ container resource to enable the RabbitMQ Delayed Message Exchange plugin.
    /// </summary>
    /// <param name="builder">The <see cref="IResourceBuilder{T}"/>.</param>
    /// <param name="tag">E.g. "4.0". Default is "latest"</param>
    public static IResourceBuilder<RabbitMQServerResource> WithDelayedMessageExchangePlugin(this IResourceBuilder<RabbitMQServerResource> builder, string tag = "latest")
    {
        ArgumentNullException.ThrowIfNull(builder);

        var dockerfilePath = GetGeneratedDockerfilePath();
        EnsureDockerfileExists(dockerfilePath, tag);

        var containerAnnotations = builder.Resource.Annotations.OfType<ContainerImageAnnotation>().ToList();

        if (containerAnnotations.Count != 1 ||
            containerAnnotations[0].Registry != _registry ||
            !string.Equals(containerAnnotations[0].Image, _image, StringComparison.OrdinalIgnoreCase))
        {
            throw new DistributedApplicationException(
                $"Cannot configure the RabbitMQ resource '{builder.Resource.Name}' to enable the Delayed Delivery plugin because it uses an unrecognized container image registry, name, or tag.");
        }

        var annotation = containerAnnotations[0];
        var existingTag = annotation.Tag;

        if (existingTag is not null && !existingTag.Contains(_delayedPluginTag, StringComparison.OrdinalIgnoreCase))
        {
            annotation.Tag = $"{existingTag}{_delayedPluginTag}";
        }

        builder.WithDockerfile(Path.GetDirectoryName(dockerfilePath)!, Path.GetFileName(dockerfilePath));

        builder.WithEnvironment(context =>
        {
            context.EnvironmentVariables["RABBITMQ_PLUGINS_DIR"] = "/opt/rabbitmq/plugins";
            context.EnvironmentVariables["RABBITMQ_ENABLED_PLUGINS_FILE"] = "/etc/rabbitmq/enabled_plugins";
        });

        builder.WithCommand("enable-delayed-plugin", "Enable Delayed Plugin",
       async (context) =>
       {
           var dockerCommand = "rabbitmq-plugins enable rabbitmq_delayed_message_exchange --offline";
           var result = await Task.Run(() => ExecuteDockerCommand(dockerCommand, builder.Resource.Name)).ConfigureAwait(false);
           return result ? CommandResults.Success() : new ExecuteCommandResult { Success = false, ErrorMessage = "Failed to enable delayed plugin." };
       });

        return builder;
    }

    private static void EnsureDockerfileExists(string dockerfilePath, string tag)
    {
        if (!File.Exists(dockerfilePath))
        {
            var dockerfileContent = $@"ARG BASE_IMAGE=rabbitmq:{tag}-management
                                    FROM ${{BASE_IMAGE}} AS base

                                    RUN apt-get update && apt-get install -y curl unzip
                                    RUN mkdir -p /opt/rabbitmq/plugins
                                    RUN curl -L -o /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-4.0.2.ez \
                                        https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v4.0.2/rabbitmq_delayed_message_exchange-4.0.2.ez
                                    RUN chmod 644 /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-4.0.2.ez

                                    FROM ${{BASE_IMAGE}}
                                    COPY --from=base /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-4.0.2.ez /opt/rabbitmq/plugins/
                                    RUN ls -lah /opt/rabbitmq/plugins/
                                    RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange";

            Directory.CreateDirectory(Path.GetDirectoryName(dockerfilePath)!);
            File.WriteAllText(dockerfilePath, dockerfileContent);
        }
    }

    private static bool ExecuteDockerCommand(string command, string containerName)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"exec {containerName} {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
            return false;
        }
    }

    private static string GetGeneratedDockerfilePath()
    {
        var tempDir = Path.Combine(AppContext.BaseDirectory, ".aspire", "GeneratedDockerfiles");
        Directory.CreateDirectory(tempDir);
        return Path.Combine(tempDir, "Dockerfile.rabbitmq");
    }
}
