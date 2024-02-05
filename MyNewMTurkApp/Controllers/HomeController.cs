// Controllers/HomeController.cs
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.MTurk;
using Amazon.MTurk.Model;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Extensions.NETCore.Setup;
using System.Reflection;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public HomeController(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult CreateHITForm()
    {
        return File("~/CreateHITForm.html", "text/html");
    }

    [HttpPost]
    public async Task<IActionResult> CreateHIT()
    {
        try
        {
            var name = Request.Form["name"];
            var description = Request.Form["description"];
            var image = Request.Form.Files["image"];

            Console.WriteLine("Imagem a ser submetida");
            if (image == null)
            {
                // Handle the case where no file was uploaded
                // This could involve returning an error message to the user, for example
                return BadRequest("No image file was uploaded.");
            }
            var key_Name = Guid.NewGuid().ToString();
            var s3ImageUrl = await UploadImageToS3(image, key_Name);
            Console.WriteLine("Imagem Submetida");

            Console.WriteLine($"Name: {name}");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine($"Image: {image.FileName}");
            
            string awsAccessKeyId = "AKIASAKFOUXBNFUHFQG7";
            string awsSecretAccessKey = "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";
            string SANDBOX_URL = "https://mturk-requester-sandbox.us-east-1.amazonaws.com";
            string PROD_URL = "https://mturk-requester.us-east-1.amazonaws.com";

            // Use the Sandbox URL
            AmazonMTurkConfig config = new AmazonMTurkConfig();
            config.ServiceURL = SANDBOX_URL;

            AmazonMTurkClient mturkClient = new AmazonMTurkClient(
                awsAccessKeyId,
                awsSecretAccessKey,
                config);

            GetAccountBalanceRequest request = new GetAccountBalanceRequest();
            GetAccountBalanceResponse balance = await mturkClient.GetAccountBalanceAsync(request);

            Console.WriteLine("Your account balance in the Sandbox is $" + balance.AvailableBalance);

            // Read the XML question file into a string
            string questionXML = System.IO.File.ReadAllText(@"F:\Universidade\Mestrado\1 Ano\1 Sem\Crowdsourcing\ProjectoMvc\MyNewMTurkApp\Question.xml");
            //var key_Name = Guid.NewGuid().ToString();
            string image_url = "https://catflora.s3.amazonaws.com/" + key_Name;
            Console.WriteLine("Link da imagem:" + image_url);
            string projectId = "3ES7ZYWJETX8M3KRRY8XHH9AKHIHC5";
            string rewardAmount = "0.01"; // Replace with your desired reward amount
            long assignmentDurationInSeconds = 60 * 60; // Replace with your desired assignment duration

            // Create individual HITs within the project using RequesterAnnotation
            CreateHITRequest hitRequest = new CreateHITRequest
            {
                RequesterAnnotation = projectId, // Use RequesterAnnotation to associate with the project
                Title = "CatFlora_PlantImageEvaluation",
                Description = "Avaliação de imagens de plantas pelos jogadores",
                //Question = questionXML,
                Reward = rewardAmount,
                AssignmentDurationInSeconds = assignmentDurationInSeconds,
                LifetimeInSeconds = 60 * 60 * 24, // 1 day
                MaxAssignments = 1, // Set the number of assignments as needed
                Question = questionXML.Replace("${image_url}", image_url)
                          .Replace("${owner}", name)
                          .Replace("${description}", description)
            };


            CreateHITResponse hit = await mturkClient.CreateHITAsync(hitRequest);

            // Show a link to the HIT within the project
            Console.WriteLine("View your project in the sandbox: https://requestersandbox.mturk.com/projects/" + hit.HIT.HITTypeId);

            // After creating the HIT, retrieve the assignments
            ListAssignmentsForHITRequest listRequest = new ListAssignmentsForHITRequest
            {
                HITId = hit.HIT.HITId,
                MaxResults = 100 // Set the maximum number of assignments to retrieve
            };

            ListAssignmentsForHITResponse listResponse = await mturkClient.ListAssignmentsForHITAsync(listRequest);
            Console.WriteLine($"Number of assignments retrieved: {listResponse.Assignments.Count}");
            Console.WriteLine("Pre-Respostas");
            foreach (var assignment in listResponse.Assignments)
            {
                // Process each assignment and retrieve the results
                var workerId = assignment.WorkerId;
                var answerContent = assignment.Answer;

                // Process the answers and store the results as needed
                Console.WriteLine($"Worker ID: {workerId}");
                Console.WriteLine($"Answers: {answerContent}");
                Console.WriteLine("Respostas");
            }

            return Json(new { message = "HIT created successfully!" });
            
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error creating HIT: {ex.Message}");
            return Json(new { error = "Failed to create HIT. Check server logs for details." });
        }
    }
    
    private async Task<IActionResult> UploadImageToS3(IFormFile file, string keyName)
    {
        var bucketName = "catflora";
        //var keyName = Guid.NewGuid().ToString();
        Console.WriteLine("Fui Chamado");
        try
        {
            using (var client = new AmazonS3Client("AKIASAKFOUXBNFUHFQG7",
            "mNmGpBH6lHyFn2UmvGagqdVGODdUiWSvsE9u7Oeq",
            Amazon.RegionEndpoint.USEast1))
        {
            using (var newMemoryStream = new MemoryStream())
            {
                file.CopyTo(newMemoryStream);

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = newMemoryStream,
                    Key = keyName,
                    BucketName = bucketName,
                    ContentType = "image/png"
                };

                var fileTransferUtility = new TransferUtility(client);
                await fileTransferUtility.UploadAsync(uploadRequest);
            }
        }
            return Ok($"https://{bucketName}.s3.amazonaws.com/{keyName}");
        }
        catch (Exception ex)
        {
            // Log the exception or return an error response
            Console.WriteLine(ex.Message);
            return BadRequest("An error occurred while uploading the image.");
        }
    }

}
