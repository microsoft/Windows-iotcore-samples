# UniversalMediaEngine
This is an IMFMediaEngine (Windows Media Foundation management class) wrapper that simplifies the playing of media 
in a Windows IoT Core headless applicaiton (since the XAML MediaElement is not avaliable to developers here).

##Usage:

* Either build/add the Windows Runtime Component as a binary reference to your solution of add the UniversalMediaEngine project to your solution.
* Initialize an instance of the MediaEngine object in your code like so:
```
            this.mediaEngine = new MediaEngine();
            var result = await this.mediaEngine.InitializeAsync();
            if (result == MediaEngineInitializationResult.Fail)
            {
                // Your error logic           
            }
```
* The MediaEngine object exposes Play (you pass a valid URL), Pause and Volume set/get as well as a callback that is fired when the state of media playback changes.
 
***

This project has adopted the [Microsoft Open Source Code of Conduct](http://microsoft.github.io/codeofconduct). For more information see the [Code of Conduct FAQ](http://microsoft.github.io/codeofconduct/faq.md) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
