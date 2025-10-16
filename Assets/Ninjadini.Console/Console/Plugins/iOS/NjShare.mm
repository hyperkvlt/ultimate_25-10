#import <UIKit/UIKit.h>

extern "C" void NjConsole_ShareFile(const char* filepath)
 {
     NSString* filePath = [NSString stringWithUTF8String:filepath];
     NSURL* fileURL = [NSURL fileURLWithPath:filePath];
     if (![[NSFileManager defaultManager] fileExistsAtPath:filePath]) return;
     dispatch_async(dispatch_get_main_queue(), ^{
         UIViewController* rootVC = [UIApplication sharedApplication].keyWindow.rootViewController;
         UIActivityViewController* activityVC = [[UIActivityViewController alloc] initWithActivityItems:@[fileURL] applicationActivities:nil];
         [rootVC presentViewController:activityVC animated:YES completion:nil];
     });
 }