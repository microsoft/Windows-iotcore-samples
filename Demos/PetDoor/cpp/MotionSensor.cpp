#include "pch.h"
#include "MotionSensor.h"
#include "TimeSpanHelper.h"

using namespace Microsoft::IoT::Lightning::Providers;

using namespace Windows::Devices;

namespace PetDoor
{

	// pin: GPIO pin connected to the motion sensor
	MotionSensor::MotionSensor(int pin)
	{
		if (LightningProvider::IsLightningEnabled)
		{
			LowLevelDevicesController::DefaultProvider = LightningProvider::GetAggregateProvider();
		}
		else
		{
			throw ref new Platform::Exception(E_FAIL, "Lightning is not enabled in this device.");
		}
		auto gpio = GpioController::GetDefault();

		if (gpio == nullptr)
		{
			throw ref new Platform::Exception(S_FALSE, "There is no GPIO controller on this device.");
		}

		_pin = gpio->OpenPin(pin);
		_pin->SetDriveMode(GpioPinDriveMode::Input);

		Windows::Foundation::TimeSpan duration = { TimeSpanHelper::FromMilliseconds(50).get_Ticks() };
		_pin->DebounceTimeout = duration;

		_pin->ValueChanged += ref new TypedEventHandler<GpioPin^, GpioPinValueChangedEventArgs^>(this, &MotionSensor::Pin_ValueChanged);
	}

	// event handler for when the motion sensor triggers
	void MotionSensor::Pin_ValueChanged(GpioPin ^sender, GpioPinValueChangedEventArgs ^e)
	{
		_pinValue = _pin->Read();
		// Motion detected, fire the event
		if (_pinValue == GpioPinValue::High)
		{
			MotionDetected(this, L"Motion detected\n");
		}
	}

	GpioPinValue MotionSensor::GetPinValue()
	{
		return _pin->Read();
	}
}