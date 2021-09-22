using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PS.WebService.Library
{
    public class MediaServicesManagementClient
    {
        private ConfigWrapper m_Config;
        private IAzureMediaServicesClient m_Client;

        public MediaServicesManagementClient(ConfigWrapper config)
        {
            m_Config = config;
        }

        public async Task Connect()
        {
            try
            {
                m_Client = await CreateMediaServicesClientAsync(m_Config);
                Console.WriteLine("connected");
            }
            catch (Exception exception)
            {
                if (exception.Source.Contains("ActiveDirectory"))
                {
                    Console.Error.WriteLine("TIP: Make sure that you have filled out the appsettings.json file before running this sample.");
                }

                Console.Error.WriteLine($"{exception.Message}");

                ApiErrorException apiException = exception.GetBaseException() as ApiErrorException;
                if (apiException != null)
                {
                    Console.Error.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }

                throw exception;
            }
        }

        public async Task<List<Asset>> GetAssetsAsync()
        {
            var assetsList = new List<Asset>();

            // List assets
            var assets = await m_Client.Assets.ListAsync
                (m_Config.ResourceGroup, m_Config.AccountName);

            foreach (var asset in assets)
            {
                assetsList.Add(asset);
            }
            return assetsList;
        }

        public async Task<Asset> CreateInputAssetAsync(string assetName, string fileToUpload)
        {
            Asset asset = await m_Client.Assets.CreateOrUpdateAsync
                (m_Config.ResourceGroup, m_Config.AccountName, assetName, new Asset());

            var response = await m_Client.Assets.ListContainerSasAsync(
                m_Config.ResourceGroup,
                m_Config.AccountName,
                assetName,
                permissions: AssetContainerPermission.ReadWrite,
                expiryTime: DateTime.UtcNow.AddHours(4).ToUniversalTime());

            var sasUri = new Uri(response.AssetContainerSasUrls.First());

            CloudBlobContainer container = new CloudBlobContainer(sasUri);
            var blob = container.GetBlockBlobReference(Path.GetFileName(fileToUpload));

            await blob.UploadFromFileAsync(fileToUpload);

            return asset;
        }

        public async Task<Asset> CreateOutputAssetAsync(string assetName)
        {
            // Check if an Asset already exists
            Asset outputAsset = await m_Client.Assets.GetAsync(m_Config.ResourceGroup, m_Config.AccountName, assetName);
            Asset asset = new Asset();
            string outputAssetName = assetName;

            if (outputAsset != null)
            {
                // Name collision! In order to get the sample to work, let's just go ahead and create a unique asset name
                // Note that the returned Asset can have a different name than the one specified as an input parameter.
                // You may want to update this part to throw an Exception instead, and handle name collisions differently.
                string uniqueness = $"-{Guid.NewGuid().ToString("N")}";
                outputAssetName += uniqueness;

                Console.WriteLine("Warning – found an existing Asset with name = " + assetName);
                Console.WriteLine("Creating an Asset with this name instead: " + outputAssetName);
            }

            return await m_Client.Assets.CreateOrUpdateAsync(m_Config.ResourceGroup, m_Config.AccountName, outputAssetName, asset);
        }

        public async Task DeleteAllAssetsAsync()
        {
            var assets = await GetAssetsAsync();

            foreach (var asset in assets)
            {
                Console.WriteLine($"Deleting { asset.Name }");
                await DeleteAssetAsync(asset.Name);
            }
        }

        public async Task DeleteAssetAsync(string assetName)
        {
            await m_Client.Assets.DeleteAsync(m_Config.ResourceGroup, m_Config.AccountName, assetName);
        }

        public async Task<Job> SubmitJobAsync(string jobInputAssetName, string transformName, string outputAssetName, string jobName)
        {

            var jobInput = new JobInputAsset(jobInputAssetName);


            JobOutput[] jobOutputs =
            {
                new JobOutputAsset(outputAssetName)
            };

            Job job;
            try
            {
                job = await m_Client.Jobs.CreateAsync(
                            m_Config.ResourceGroup,
                            m_Config.AccountName,
                            transformName,
                            jobName,
                            new Job
                            {
                                Input = jobInput,
                                Outputs = jobOutputs,
                            });
            }
            catch (Exception exception)
            {
                if (exception.GetBaseException() is ApiErrorException apiException)
                {
                    Console.Error.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }
                throw exception;
            }

            return job;
        }

        public async Task<Job> SubmitAnalysisJobAsync(string jobInputAssetName, string transformName, string jobName)
        {

            await CreateOutputAssetAsync($"{ jobInputAssetName }_AudioAnalyzer");
            await CreateOutputAssetAsync($"{ jobInputAssetName }_FaceDetector");
            await CreateOutputAssetAsync($"{ jobInputAssetName }_VideoAnalyzer");

            var jobInput = new JobInputAsset(jobInputAssetName);

            JobOutput[] jobOutputs =
            {
                new JobOutputAsset($"{ jobInputAssetName }_AudioAnalyzer"),
                new JobOutputAsset($"{ jobInputAssetName }_FaceDetector"),
                new JobOutputAsset($"{ jobInputAssetName }_VideoAnalyzer")
            };

            Job job;
            try
            {
                job = await m_Client.Jobs.CreateAsync(
                            m_Config.ResourceGroup,
                            m_Config.AccountName,
                            transformName,
                            jobName,
                            new Job
                            {
                                Input = jobInput,
                                Outputs = jobOutputs,
                            });
            }
            catch (Exception exception)
            {
                if (exception.GetBaseException() is ApiErrorException apiException)
                {
                    Console.Error.WriteLine(
                        $"ERROR: API call failed with error code '{apiException.Body.Error.Code}' and message '{apiException.Body.Error.Message}'.");
                }
                throw exception;
            }

            return job;
        }

        public async Task<Job> WaitForJobToFinishAsync(string transformName, string jobName)
        {
            const int SleepIntervalMs = 10 * 1000;

            Job job = null;

            do
            {
                job = await m_Client.Jobs.GetAsync(m_Config.ResourceGroup, m_Config.AccountName, transformName, jobName);

                Console.WriteLine($"Job is '{job.State}'.");
                for (int i = 0; i < job.Outputs.Count; i++)
                {
                    JobOutput output = job.Outputs[i];
                    Console.Write($"\tJobOutput[{i}] is '{output.State}'.");
                    if (output.State == JobState.Processing)
                    {
                        Console.Write($"  Progress: '{output.Progress}'.");
                    }

                    Console.WriteLine();
                }

                if (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled)
                {
                    await Task.Delay(SleepIntervalMs);
                }
            }
            while (job.State != JobState.Finished && job.State != JobState.Error && job.State != JobState.Canceled);

            return job;
        }

        public async Task<StreamingLocator> CreateStreamingLocatorAsync(string assetName, string locatorName)
        {
            StreamingLocator locator = 
                await m_Client.StreamingLocators.CreateAsync(
                m_Config.ResourceGroup,
                m_Config.AccountName,
                locatorName,
                new StreamingLocator
                {
                    AssetName = assetName,
                    StreamingPolicyName = 
                    PredefinedStreamingPolicy.DownloadAndClearStreaming
                });

            return locator;
        }

        public async Task<List<string>> GetStreamingUrlsAsync(String locatorName)
        {
            const string DefaultStreamingEndpointName = "default";

            List<string> streamingUrls = new List<string>();

            StreamingEndpoint streamingEndpoint = await m_Client.StreamingEndpoints.GetAsync
                (m_Config.ResourceGroup, m_Config.AccountName, DefaultStreamingEndpointName);

            if (streamingEndpoint != null)
            {
                if (streamingEndpoint.ResourceState != StreamingEndpointResourceState.Running)
                {
                    await m_Client.StreamingEndpoints.StartAsync
                        (m_Config.ResourceGroup, m_Config.AccountName, DefaultStreamingEndpointName);
                }
            }

            ListPathsResponse paths = await m_Client.StreamingLocators.ListPathsAsync
                (m_Config.ResourceGroup, m_Config.AccountName, locatorName);

            foreach (StreamingPath path in paths.StreamingPaths)
            {
                UriBuilder uriBuilder = new UriBuilder();
                uriBuilder.Scheme = "https";
                uriBuilder.Host = streamingEndpoint.HostName;

                uriBuilder.Path = path.Paths[0];
                streamingUrls.Add(uriBuilder.ToString());
            }

            return streamingUrls;
        }

        public async Task<List<Transform>> GetTransformsAsync()
        {
            var transormsList = new List<Transform>();
            var transorms = await m_Client.Transforms.ListAsync(m_Config.ResourceGroup, m_Config.AccountName);


            foreach (var transform in transorms)
            {
                transormsList.Add(transform);
                //Console.WriteLine($"{ transform.Name }");
            }

            return transormsList;
        }

        public async Task<Transform> GetOrCreateTransformAsync(EncoderNamedPreset encoderNamedPreset, string transformName)
        {
            
            Transform transform = await m_Client.Transforms.GetAsync
                (m_Config.ResourceGroup, m_Config.AccountName, transformName);

            if (transform == null)
            {
                Console.WriteLine($"Creating Transform: { transformName }");
                TransformOutput[] output = new TransformOutput[]
                {
                    new TransformOutput
                    {
                        Preset = new BuiltInStandardEncoderPreset()
                        {
                            PresetName = encoderNamedPreset
                        }
                    }
                };

                transform = await m_Client.Transforms.CreateOrUpdateAsync
                    (m_Config.ResourceGroup, m_Config.AccountName, transformName, output);
            }

            return transform;
        }

        public async Task<Transform> GetOrCreateTransformAsync(string transformName)
        {
            // Does a Transform already exist with the desired name? Assume that an existing Transform with the desired name
            // also uses the same recipe or Preset for processing content.
            Transform transform = await m_Client.Transforms.GetAsync(m_Config.ResourceGroup, m_Config.AccountName, transformName);

            if (transform == null)
            {
                // You need to specify what you want it to produce as an output
                TransformOutput[] output = new TransformOutput[]
                {
                    new TransformOutput
                    {
                        Preset = new BuiltInStandardEncoderPreset()
                        {
                            // This sample uses the built-in encoding preset for Adaptive Bitrate Streaming.
                            PresetName = EncoderNamedPreset.AdaptiveStreaming
                        }
                    }
                    // Create an analyzer preset with video insights.
                    
                    //,new TransformOutput(new AudioAnalyzerPreset("en-US"))
                    //,new TransformOutput(new FaceDetectorPreset())
                };

                // Create the Transform with the output defined above
                transform = await m_Client.Transforms.CreateOrUpdateAsync(m_Config.ResourceGroup, m_Config.AccountName, transformName, output);
            }

            return transform;
        }

        public async Task<Transform> GetOrCreateAnalysisTransformAsync(string transformName)
        {
            // Does a Transform already exist with the desired name?
            Transform transform = await m_Client.Transforms.GetAsync
                (m_Config.ResourceGroup, m_Config.AccountName, transformName);

            if (transform == null)
            {
                // Create a transform output array with audio, face and video analysis
                TransformOutput[] output = new TransformOutput[]
                {                    
                    new TransformOutput(new AudioAnalyzerPreset("en-US")),
                    new TransformOutput(new FaceDetectorPreset()),
                    new TransformOutput(new VideoAnalyzerPreset())
                };

                // Create the Transform with the output defined above
                transform = await m_Client.Transforms.CreateOrUpdateAsync
                    (m_Config.ResourceGroup, m_Config.AccountName, transformName, output);
            }

            return transform;
        }
        
        public async Task DeleteAllTransformsAsync()
        {
            var transforms = await GetTransformsAsync();

            foreach (var transform in transforms)
            {
                Console.WriteLine($"Deleting { transform.Name }");
                await DeleteTransformAsync(transform.Name);
            }
        }

        public async Task DeleteTransformAsync(string transformName)
        {
            await m_Client.Transforms.DeleteAsync(m_Config.ResourceGroup, m_Config.AccountName, transformName);
        }

        private static async Task<ServiceClientCredentials> GetCredentialsAsync(ConfigWrapper config)
        {
            ClientCredential clientCredential =
                new ClientCredential(config.AadClientId, config.AadSecret);

            return await ApplicationTokenProvider.LoginSilentAsync
                (config.AadTenantId, clientCredential, ActiveDirectoryServiceSettings.Azure);
        }

        private static async Task<IAzureMediaServicesClient> CreateMediaServicesClientAsync(ConfigWrapper config)
        {
            var credentials = await GetCredentialsAsync(config);

            return new AzureMediaServicesClient(config.ArmEndpoint, credentials)
            {
                SubscriptionId = config.SubscriptionId,
            };
        }
    }
}
