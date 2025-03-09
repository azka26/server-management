public class FileUploader
{
    public static async Task<int> UploadFileAsync(string url, FileInfo fileInfo)
    {
        using var fs = File.OpenRead(fileInfo.FullName);
        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fs);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        content.Add(streamContent, "file", fileInfo.Name);

        var progress = new Progress<long>();
        progress.ProgressChanged += (sender, bytesTransferred) =>
        {
            int progressPercentage = (int)(bytesTransferred * 100 / fileInfo.Length);
            Console.Write($"\rUpload progress {fileInfo.Name}: {progressPercentage}%");
        };

        using var httpClient = new HttpClient();
        var response = await httpClient.PostAsync(url, new ProgressableContent(content, progress));
        Console.WriteLine();

        if (response.StatusCode == System.Net.HttpStatusCode.OK) 
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Upload Status: {response.StatusCode}");
            Console.WriteLine($"Reponse: {responseBody}");
            return Convert.ToInt32(responseBody);
        }

        var responseBodyNOk = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Upload Status: {response.StatusCode}");
        Console.WriteLine($"Reponse: {responseBodyNOk}");
        throw new Exception($"Failed Upload File {fileInfo.Name}.");
    }
}
