// Copyright (c) Microsoft. All rights reserved.

#include "pch.h"
#include "StartupTask.h"

using namespace BlinkyHeadlessCpp;

using namespace Platform;
using namespace Windows::ApplicationModel::Background;
using namespace Windows::Foundation;
using namespace Windows::Devices::Gpio;
using namespace Windows::System::Threading;
using namespace concurrency;

StartupTask::StartupTask()
{
}

void StartupTask::Run(IBackgroundTaskInstance^ taskInstance)
{
	taskInstance->Canceled += ref new BackgroundTaskCanceledEventHandler(this, &StartupTask::OnCanceled);

	Deferral = taskInstance->GetDeferral();
	InitGpio();

	TimerElapsedHandler ^handler = ref new TimerElapsedHandler(
		[this](ThreadPoolTimer ^timer)
	{
		BackgroundTaskDeferral^ deferral = Deferral.Get();
		if (_cancelRequested == false) {
			pinValue = (pinValue == GpioPinValue::High) ? GpioPinValue::Low : GpioPinValue::High;
			pin->Write(pinValue);
		}
		else {
			timer->Cancel();
			//
			// Indicate that the background task has completed.
			//
			deferral->Complete();
		}
		
	});

	TimeSpan interval;
	interval.Duration = 500 * 1000 * 10;
	Timer = ThreadPoolTimer::CreatePeriodicTimer(ref new TimerElapsedHandler(handler), interval);

}

void StartupTask::InitGpio()
{
	pin = GpioController::GetDefault()->OpenPin(LED_PIN);
	pinValue = GpioPinValue::High;
	pin->Write(pinValue);
	pin->SetDriveMode(GpioPinDriveMode::Output);
}

void StartupTask::OnCanceled(IBackgroundTaskInstance^ sender, BackgroundTaskCancellationReason reason) {
	_cancelRequested = true;
	_cancelReason = reason;
}
