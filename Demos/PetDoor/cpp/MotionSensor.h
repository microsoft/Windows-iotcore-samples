#pragma once

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Devices::Enumeration;
using namespace Windows::Devices::Gpio;
using namespace Windows::UI::Xaml;
using namespace Windows::System::Threading;

namespace PetDoor
{
	public delegate void MotionDetectedEventHandler(Object^ sender, Platform::String^ s);

	public ref class MotionSensor sealed
	{
	public:
		MotionSensor(int pin);
		event MotionDetectedEventHandler^ MotionDetected;
		GpioPinValue GetPinValue();

	private:
		ThreadPoolTimer ^_timer;
		int _timerInterval;
		GpioPinValue _pinValue = Windows::Devices::Gpio::GpioPinValue::High;
		GpioPin ^_pin;
		void Pin_ValueChanged(GpioPin ^sender, GpioPinValueChangedEventArgs ^e);
	};
}
