// See https://aka.ms/new-console-template for more information
using InstagramApp;

Console.Title = "Instagram corousal post utility";
Console.WriteLine("Hello,Welcome to Instagram corousal post utility.");
Console.WriteLine("");
FacebookApp myapp = new FacebookApp();
myapp.operationAchieved += Myapp_operationAchieved;
myapp.Start();

Console.WriteLine("Post complete");

void Myapp_operationAchieved(object? sender, ResponseDTO e)
{

  //  myapp.Reprocess();
}




