$log=Get-Content Design/Cards/LevelCompleted-v12/Verification/title-buttons-checks.txt -Raw
if($log -notmatch 'SUMMARY passes=\d+ fails=0\s+DONE\s*$'){throw 'Only encode a finalized passing capture.'}
$source=@'
using System;
using System.Diagnostics;
using System.IO;
public static class RoyalReviewEncoder {
 public static string Encode(string folder,string output) {
 var decode=new Process(); var encode=new Process();
 decode.StartInfo=new ProcessStartInfo("C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe", "-hide_banner -v error -safe 0 -f concat -i capture.ffconcat -vf scale=720:1280,fps=30 -t 5 -an -f rawvideo -pix_fmt rgb24 -");
 decode.StartInfo.WorkingDirectory=folder;decode.StartInfo.UseShellExecute=false;decode.StartInfo.CreateNoWindow=true;decode.StartInfo.WindowStyle=ProcessWindowStyle.Hidden;decode.StartInfo.RedirectStandardOutput=true;decode.StartInfo.RedirectStandardError=true;
 encode.StartInfo=new ProcessStartInfo("C:/Program Files/TimeDoctor2/3.18.89/ffmpeg.exe", "-y -hide_banner -v error -f rawvideo -pix_fmt rgb24 -video_size 720x1280 -framerate 30 -i - -an -c:v libx264 -preset fast -crf 18 -pix_fmt yuv420p -movflags +faststart \"" + output + "\"");
 encode.StartInfo.UseShellExecute=false;encode.StartInfo.CreateNoWindow=true;encode.StartInfo.WindowStyle=ProcessWindowStyle.Hidden;encode.StartInfo.RedirectStandardInput=true;encode.StartInfo.RedirectStandardError=true;
 decode.Start();encode.Start();
 var de=decode.StandardError.ReadToEndAsync();var ee=encode.StandardError.ReadToEndAsync();
 try {decode.StandardOutput.BaseStream.CopyTo(encode.StandardInput.BaseStream);encode.StandardInput.Close();decode.WaitForExit();encode.WaitForExit();
 if(decode.ExitCode!=0 || encode.ExitCode!=0)throw new Exception(de.Result+" "+ee.Result);
 return output;
 }finally{if(!decode.HasExited)decode.Kill();if(!encode.HasExited)encode.Kill();decode.Dispose();encode.Dispose();}
 }
}
'@
Add-Type -TypeDefinition $source
$folder=Join-Path $pwd 'Design/Cards/LevelCompleted-v12/Verification/TitleButtons'
$output=Join-Path $pwd 'Design/Cards/LevelCompleted-v12/Royal-Throne-Celebration-v12.mp4'
[RoyalReviewEncoder]::Encode($folder,$output)
& 'C:/Program Files/Softdeluxe/Free Download Manager/ffmpeg.exe' -hide_banner -v error -i $output -f null -
if($LASTEXITCODE -ne 0){throw 'Preview decode failed'}
