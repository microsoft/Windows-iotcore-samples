Windows 10 IoT Core sample code
===============

[Documentation for this sample](https://developer.microsoft.com/en-us/windows/iot/samples/OpenCV)

## Notes:
- Make sure if you have Latest or Required Windows 10 SDK mentioned in the Project.  Right click on Project -> Properties -> General ( under Configuration Properties ) -> Target Platform Version 
- Make sure to following the Instructions provided in above link
- Compile OpenCV.sln as per link Instructions
- Then Try to Compile this current solution as there are some dependency libraries thats get used in here.


## Troubleshoot:
- error LNK1104: cannot open file 'opencv_core300d.lib'
     Can be 2 reasons
     - a. You might not have followed [OpenCV documentation](https://developer.microsoft.com/en-us/windows/iot/samples/OpenCV) and did not configure Environment Variable
     - b. You might have not compiled OpenCV.sln for respective platform for which you are trying to compile OpenCVExample.sln