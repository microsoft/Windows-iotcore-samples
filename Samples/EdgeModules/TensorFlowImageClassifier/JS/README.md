# Introduction 
Machine Learning sample for node-ChakraCore running a TensorFlow.js mobilenet image classifier.

# Getting Started
Download [node-ChakraCore release binaries](https://github.com/nodejs/node-chakracore/releases) from the github or use [NVS](https://github.com/jasongin/nvs) to install the node-ChakraCore runtime. NVS makes it easy to switch between versions like release or daily build.  
Update PATH environment variable to include the path to the node-ChakraCore binaries.  NVS does this path update on selection of which version to use.

Install the sample package  
  * npm install &lt;path to package&gt;  

TensorFlow.js mobilenet image classifier  
  * change to the sample directory  
  * node TFImageDemo.js &lt;jpg with size 224 x 224 pixels&gt;  
  * The top 3 predictions with probabilites will be shown.  

# Build and Test
The dependencies should be binary drop instead of compile/build packages.  Might need VS if some of the package installs need to build.

# Contribute
See [this section](https://github.com/microsoft/Windows-iotcore-samples#contributions)

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.