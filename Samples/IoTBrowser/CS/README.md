# IoT Browser Sample

We'll create a simple web browser application for your your Windows 10 IoT Core device.

This is a headed sample.  To better understand what headed mode is and how to configure your device to be headed, follow the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/headlessmode).

As this sample uses just standard Windows UWP features, it can also run on your desktop.

### Load the project in Visual Studio

You can find the source code for this sample by downloading a zip of all of our samples [here](https://github.com/Microsoft/Windows-iotcore-samples/archive/master.zip) and navigating to the `samples-develop\IoTBrowser`. The sample code is C#. Make a copy of the folder on your disk and open the project from Visual Studio.

Once the project is open and builds, the next step is to [deploy](https://github.com/MicrosoftDocs/windows-iotcore-docs/blob/master/windows-iotcore/develop-your-app/AppDeployment.md) the application to your device.

When everything is set up, you should be able to press F5 from Visual Studio. The IoT Browser app will deploy and start on the Windows IoT device.

### Let's look at the code
The code for this sample is pretty simple:
<ul>
<li>An embedded webview control</li>
<li>TextBox as address bar</li>
<li>Go button to start navigation</li>
<li>And three favorites buttons</li>
</ul>

When the go button is pressed, we call a web navigation helper method to do the actual navigation.

### UX code
    <Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
        <Grid.RowDefinitions>
            <RowDefinition Height="65"></RowDefinition>
            <RowDefinition Height="*"></RowDefinition>
            <RowDefinition Height="65"></RowDefinition>
        </Grid.RowDefinitions>

        <!--Address bar-->
        <StackPanel Grid.Row="0" Orientation="Horizontal">
            <TextBox x:Name="Web_Address" FontSize="24" TextWrapping="Wrap" Text="http://www.bing.com" VerticalAlignment="Center" VerticalContentAlignment="Center" Height="54" Width="958" KeyUp="Web_Address_KeyUp"/>
            <Button x:Name="Go_Web" Content="Go!" HorizontalAlignment="Right" VerticalAlignment="Center" Height="60" Width="107" Click="Go_Web_Click"/>
        </StackPanel>

        <!--Web view control-->
        <WebView x:Name="webView" Grid.Row="1"/>

        <!--Favorites buttons-->
        <StackPanel Grid.Row="2" Orientation="Horizontal" >
            <Button x:Name="Go_WOD" VerticalAlignment="Center" HorizontalAlignment="Center" Height="60" Width="120" Margin="0,0,15,0" Click="Go_WOD_Click">
                <TextBlock Text="Windows on Devices" TextWrapping="Wrap"/>
            </Button>
            <Button x:Name="Go_Hackster" Content="Hackster.io" VerticalAlignment="Center" Height="60" Width="120" Margin="0,0,15,0" Click="Go_Hackster_Click"/>
            <Button x:Name="Go_GitHub" Content="GitHub.com" VerticalAlignment="Center" Height="60" Width="120" Margin="0,0,15,0" Click="Go_GitHub_Click"/>
        </StackPanel>
    </Grid>

### DoWebNavigate navigation helper method
This helper uses the WebView.Navigate method with the value currently in the Web_Address.Text

### DoWebNavigate code
    if (Web_Address.Text.Length > 0)
    {
        webView.Navigate(new Uri(Web_Address.Text));
    }

### Favorites buttons
The three favorites simply fill the address bar text with a preconfigured value then call the DoWebNavigate helper.

### Favorite code
    private void Go_Hackster_Click(object sender, RoutedEventArgs e)
    {
        Web_Address.Text = "https://www.hackster.io/windowsiot";
        DoWebNavigate();
    }
