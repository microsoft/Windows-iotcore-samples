# TextDisplay
a text display driver framework

This project has adopted the [Microsoft Open Source Code of Conduct](http://microsoft.github.io/codeofconduct). For more information see the [Code of Conduct FAQ](http://microsoft.github.io/codeofconduct/faq.md) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.


Supported Displays:
	HD44780 powered display (4Bit mode only) over GPIO - driverType="HD44780GpioDriver"

Usage:

1. Either build/add the Windows Runtime Component as a binary reference to your solution of add the TextDisplayManager project to you solution.
2. Edit the "screens.config" file to match your setup.
3. Call:
            var displays = await TextDisplayManager.GetDisplays();
4. The 'displays' variable will contain a list of configured displays for the system (as defined in screens.config).
5. Each display implements the ITextDisplay interface that has the following fucntionality:
	get Height - Gets the total rows the display has
	get Width - Gets the total characters each row supports
	InitializeAsync - Initializes the screen
	DisposeAsync - Disposes of the screen once its use is complete
	WriteMessageAsync - Writes a message to the screen (supports \n for new line), timeout indicates how long the message will stay on screen (0 is infinite).

Adding a Driver:
	Drivers are C# classes that inherit from TextDisplayBase, these drivers must be added as a part of the TextDisplay component to be able to be activated.

screens.config:
	This xml configuration file describes the screens that are currently attached to the system.
	Each Screen element is broken into 2 main elements, CommonConfiguration and DriverConfiguration.
	CommonConfiguration contians the Heigh and Width of the screen
	DriverConfiguration contains a XML fragment that is passsed into the driver on initialization
	Below is an example of a screens.config that has 1 HD44780GpioDriver driven screen:


<?xml version="1.0" encoding="utf-8" ?>
<Screens>
  <Screen driverType="HD44780GpioDriver">
    <CommonCofiguration>
      <Height>2</Height>
      <Width>16</Width>
    </CommonCofiguration>
    <DriverConiguration>
      <RsPin>18</RsPin>
      <EnablePin>23</EnablePin>
      <D4Pin>24</D4Pin>
      <D5Pin>5</D5Pin>
      <D6Pin>6</D6Pin>
      <D7Pin>13</D7Pin>
    </DriverConiguration>
  </Screen>
</Screens>

