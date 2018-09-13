# Medical Heart Disease Prediction

For doctors and nurses that are constantly on the run, clipboards and other unwieldy devices can be cumbersome to carry around. Not to mention, the spread of germs is made easy when there are so many devices and papers to handle and pass around.

This medical tablet is specifically designed for doctors and nurses. The model that was used for this project had the model number AIM-55, which is from Advantech and can be bought [here](http://www.advantech.com/products/1-2zydkr/aim-55/mod_3ffb2e2f-4a06-4db7-a0e7-5840c353b6a6). It can be easily held with one hand and fits in a typical medical robe pocket. The tablet is housed in an anti-bacterial shell. 

This sample includes 1) the UWP project and 2) code for the ML model.

![Diagram of solution components](../../../Resources/images/AdvantechMedicalHeart/Medical_WinML.jpg)

A UWP app is designed for heart disease prediction based on the data set from UC Irvine. This data set can be found [here](https://archive.ics.uci.edu/ml/datasets/heart+Disease). We use CNTK to train the model and port it to ONNX (Open Neural Network Exchange) format for Windows ML to consume on the tablet device. With Windows ML, the prediction can be done directly on the edge device without network connection, which is critical for hospital environments. 
