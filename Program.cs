// See https://aka.ms/new-console-template for more information
using InstagramApp;

Console.WriteLine("Hello, Ready!");
FacebookApp myapp = new FacebookApp();
Console.WriteLine("Please input folder path");
object folderpath = Console.ReadLine();
if (folderpath != null)
{
    IEnumerable<string> files = Directory.EnumerateFiles(folderpath.ToString(), "*.png");
    myapp.files = files.ToList();

    Console.WriteLine("Connecting Instagram for security code");
    ResponseDTO dto = myapp.GetCode();
    Console.WriteLine("Go to the link {0}, enter the security code {1}", dto.verification_uri, dto.user_code);
    Console.WriteLine("Waiting for authorization");
     myapp.StartAuthorizationCheck(dto);
    Console.ReadLine();
    
}


