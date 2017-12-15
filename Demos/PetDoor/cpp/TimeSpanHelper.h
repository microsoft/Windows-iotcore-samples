#pragma once
namespace PetDoor
{
	class TimeSpanHelper
	{
	public:
		TimeSpanHelper()
			: m_ticks(0)
		{
		}

		static inline TimeSpanHelper FromTicks(__int64 ticks)
		{
			TimeSpanHelper timeSpan;
			timeSpan.m_ticks = ticks;

			return timeSpan;
		}

		static inline TimeSpanHelper FromMilliseconds(__int64 value)
		{
			return TimeSpanHelper::FromTicks(value * TicksPerMillisecond);
		}

		static inline TimeSpanHelper FromSeconds(__int64 value)
		{
			return TimeSpanHelper::FromTicks(value * TicksPerSecond);
		}

		static inline TimeSpanHelper FromDays(__int64 value)
		{
			return TimeSpanHelper::FromTicks(value * TicksPerDay);
		}

		static inline TimeSpanHelper FromHours(__int64 value)
		{
			return TimeSpanHelper::FromTicks(value * TicksPerHour);
		}

		static inline TimeSpanHelper FromMinutes(__int64 value)
		{
			return TimeSpanHelper::FromTicks(value * TicksPerMinute);
		}

		static inline TimeSpanHelper MaxValue()
		{
			return TimeSpanHelper::FromTicks(static_cast<__int64>(-1));
		}

		static inline TimeSpanHelper Zero()
		{
			return TimeSpanHelper::FromTicks(0);
		}

		int inline get_Milliseconds() const
		{
			return static_cast<int>((m_ticks / TicksPerMillisecond) % 1000);
		}

		int inline get_Seconds() const
		{
			return static_cast<int>((m_ticks / TicksPerSecond) % 60);
		}

		int inline get_Minutes() const
		{
			return static_cast<int>((m_ticks / TicksPerMinute) % 60);
		}

		int inline get_Hours() const
		{
			return static_cast<int>((m_ticks / TicksPerHour) % 24);
		}

		int inline get_Days() const
		{
			return static_cast<int>(m_ticks / TicksPerDay);
		}

		__int64 inline get_Ticks() const
		{
			return m_ticks;
		}

		__int64 inline get_TotalMilliseconds() const
		{
			return (m_ticks / TicksPerMillisecond);
		}

		__int64 inline get_TotalSeconds() const
		{
			return (m_ticks / TicksPerSecond);
		}

		/*std::wstring inline ToString() const
		{
		std::wostringstream toStringStream;

		if (this->get_Ticks() >= TicksPerDay)
		{
		toStringStream << this->get_Days() << L'.';
		}

		toStringStream << std::setfill(L'0') << std::setw(2) << this->get_Hours() << L':';
		toStringStream << std::setfill(L'0') << std::setw(2) << this->get_Minutes() << L':';
		toStringStream << std::setfill(L'0') << std::setw(2) << this->get_Seconds() << L'.';
		toStringStream << std::setfill(L'0') << std::setw(3) << this->get_Milliseconds();

		return toStringStream.str();
		}*/

		bool operator==(const TimeSpanHelper& timeSpan) const
		{
			return m_ticks == timeSpan.m_ticks;
		}

		bool operator!=(const TimeSpanHelper& timeSpan) const
		{
			return m_ticks != timeSpan.m_ticks;
		}

		bool operator<(const TimeSpanHelper& timeSpan) const
		{
			return m_ticks < timeSpan.m_ticks;
		}

		bool operator>(const TimeSpanHelper& timeSpan) const
		{
			return m_ticks > timeSpan.m_ticks;
		}

		bool operator>=(const TimeSpanHelper& timeSpan) const
		{
			return m_ticks >= timeSpan.m_ticks;
		}

		bool operator<=(const TimeSpanHelper& timeSpan) const
		{
			return m_ticks <= timeSpan.m_ticks;
		}

		TimeSpanHelper Add(const TimeSpanHelper& timeSpan)
		{
			return TimeSpanHelper::FromTicks(m_ticks + timeSpan.m_ticks);
		}

		TimeSpanHelper Subtract(const TimeSpanHelper& timeSpan)
		{
			return TimeSpanHelper::FromTicks(m_ticks - timeSpan.m_ticks);
		}

		operator __int64() { return m_ticks; }

		static const __int64 TicksPerMillisecond = 10000;
		static const __int64 TicksPerSecond = TicksPerMillisecond * 1000;
		static const __int64 TicksPerMinute = TicksPerSecond * 60;
		static const __int64 TicksPerHour = TicksPerMinute * 60;
		static const __int64 TicksPerDay = TicksPerHour * 24;

	private:

		__int64 m_ticks;
	};
}