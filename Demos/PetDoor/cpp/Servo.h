#pragma once

#include <Lightning.h>

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Devices::Enumeration;
using namespace Windows::Devices::Gpio;
using namespace Windows::UI::Xaml;
using namespace Windows::System::Threading;
using namespace Windows::Devices::Pwm;

#define SERVO_FREQUENCY 50

namespace PetDoor
{
	public ref class Servo sealed
	{
	public:
		Servo(int pin);
		void Rotate(double dutyCyclePercentage);
		void Stop();
	private:
		PwmPin ^_pin;
	};
}