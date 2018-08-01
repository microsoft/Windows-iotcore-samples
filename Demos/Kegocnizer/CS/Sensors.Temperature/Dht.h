// Copyright © 2015 Daniel Porrey
//
// This file is part of the Dht Solution.
// 
// Dht Solution is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dht Solution is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Dht Solution. If not, see http://www.gnu.org/licenses/.
//
#pragma once

#define SAMPLE_HOLD_LOW_MILLIS 18
#define DEFAULT_MAX_RETRIES 20

namespace Sensors
{
	namespace Temperature
	{
		public value struct DhtReading
		{
			bool TimedOut;
			bool IsValid;
			double Temperature;
			double Humidity;
			int RetryCount;
		};

		public interface class IDht
		{
			Windows::Foundation::IAsyncOperation<DhtReading>^ GetReadingAsync();
			Windows::Foundation::IAsyncOperation<DhtReading>^ GetReadingAsync(int maxRetries);
		};

		public ref class Dht11 sealed : IDht
		{
			public:
			Dht11(Windows::Devices::Gpio::GpioPin^ pin, Windows::Devices::Gpio::GpioPinDriveMode inputReadMode);
			virtual ~Dht11();

			virtual Windows::Foundation::IAsyncOperation<DhtReading>^ GetReadingAsync();
			virtual Windows::Foundation::IAsyncOperation<DhtReading>^ GetReadingAsync(int maxRetries);

			private:
			Windows::Devices::Gpio::GpioPinDriveMode _inputReadMode;
			Windows::Devices::Gpio::GpioPin^ _pin;

			DhtReading InternalGetReading();
			DhtReading Dht11::CalculateValues(std::bitset<40> bits);
		};

		public ref class Dht22 sealed : IDht
		{
			public:
			Dht22(Windows::Devices::Gpio::GpioPin^ pin, Windows::Devices::Gpio::GpioPinDriveMode inputReadMode);
				virtual ~Dht22();

				virtual Windows::Foundation::IAsyncOperation<DhtReading>^ GetReadingAsync();
				virtual Windows::Foundation::IAsyncOperation<DhtReading>^ GetReadingAsync(int maxRetries);

			private:
				Windows::Devices::Gpio::GpioPinDriveMode _inputReadMode;
				Windows::Devices::Gpio::GpioPin^ _pin;

				DhtReading InternalGetReading();
				DhtReading Dht22::CalculateValues(std::bitset<40> bits);
		};
	}
}