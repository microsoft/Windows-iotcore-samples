// Copyright (c) Microsoft. All rights reserved.
//
// PwmTestTool
//
//   Utility to read and write pwm devices from the command line.
//   Shows how to use C++/CX in console applications.
//

#include <ppltasks.h>
#include <collection.h>
#include <string>
#include <vector>
#include <sstream>
#include <iostream>
#include <cwctype>

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Devices::Pwm;

class wexception
{
public:
    explicit wexception (const std::wstring &msg) : msg_(msg) { }
    virtual ~wexception () { /*empty*/ }

    virtual const wchar_t *wwhat () const
    {
        return msg_.c_str();
    }

private:
    std::wstring msg_;
};

void ListPwmControllers ()
{
    using namespace Windows::Devices::Enumeration;
    using namespace Platform::Collections;

    std::wcout << L"Finding PwmControllers\n";

    String^ friendlyNameProperty = L"System.Devices.SchematicName";
    auto properties = ref new Vector<String^>();
    properties->Append(friendlyNameProperty);

    auto dis = concurrency::create_task(DeviceInformation::FindAllAsync(
        PwmController::GetDeviceSelector(), properties)).get();
    if (dis->Size < 1) {
        std::wcout << L"There are no pwm controllers on this system.\n";
        return;
    }

    std::wcout << L"Found " << dis->Size << L" PwmControllers\n";

    std::wcout << L"  SchematicName Id pincount minFrequency maxFrequency\n";
    for (const auto& di : dis) {
        String^ id = di->Id;
        String^ friendlyName = L"<null>";

        auto prop = di->Properties->Lookup(friendlyNameProperty);
        if (prop != nullptr) {
            friendlyName = prop->ToString();
        }

        std::wcout << L"  " << friendlyName->Data() << L" " << id->Data() << L" ";

        auto device = concurrency::create_task(PwmController::FromIdAsync(
            id)).get();

        if (!device) {
            std::wostringstream msg;
            std::wcout << L"Pwm controller " << id->Data() <<
                L" is in use.\n";
            continue;
        }

        std::wcout << device->PinCount << L" " << device->MinFrequency << L" " << device->MaxFrequency << L"\n";
    }
}

PwmController^ MakeDevice (_In_opt_ String^ friendlyName)
{
    using namespace Windows::Devices::Enumeration;

    String^ aqs;
    String^ id;

    if (friendlyName) {
        aqs = PwmController::GetDeviceSelector(friendlyName);
    }
    else {
        aqs = PwmController::GetDeviceSelector();
    }

    auto dis = concurrency::create_task(DeviceInformation::FindAllAsync(aqs)).get();
    if (dis->Size > 0) {
        id = dis->GetAt(0)->Id;
    }
    else if (friendlyName->Length() >= 2 &&
             friendlyName->ToString()->Data()[0] == L'\\' &&
             friendlyName->ToString()->Data()[1] == L'\\') {
        id = friendlyName;
    }
    else {
        throw wexception(L"pwm controller not found");
    }

    auto device = concurrency::create_task(PwmController::FromIdAsync(
                    id)).get();

    if (!device) {
        std::wostringstream msg;
        msg << L"Pwm controller " << id->Data() <<
            L" is in use. Please ensure that no other applications are using pwm.";
        throw wexception(msg.str());
    }

    return device;
}

std::wostream& operator<< (std::wostream& os, PwmPulsePolarity polarity)
{
    switch (polarity) {
    case PwmPulsePolarity::ActiveHigh:
        return os << L"ActiveHigh";
    case PwmPulsePolarity::ActiveLow:
        return os << L"ActiveLow";
    default:
        return os << L"[Invalid polarity]";
    }
}

PCWSTR Help =
    L"Commands:\n"
    L" > freq <f>                         Set controller frequency (Hz)\n"
    L" > open <pin>                       Open pin\n"
    L" > start                            start PWM\n"
    L" > stop                             stop PWM\n"
    L" > dutycycle <percentage>           set duty cycle percentage\n"
    L" > polarity                         toggle polarity\n"
    L" > info                             Display device information\n"
    L" > help                             Display this help message\n"
    L" > quit                             Quit\n\n";

void ShowPrompt (PwmController^ device)
{
    PwmPin^ pin;

    while (std::wcin) {
        std::wcout << L"> ";

        std::wstring line;
        if (!std::getline(std::wcin, line)) {
            return;
        }

        std::wistringstream linestream(line);
        std::wstring command;

        linestream >> command;
        if ((command == L"q") || (command == L"quit")) {
            return;
        } else if ((command == L"h") || (command == L"help")) {
            std::wcout << Help;
        } else if (command == L"open") {
            // open pin N
            unsigned int pinId;
            if (!(linestream >> std::dec >> pinId)) {
                std::wcout << L"Expecting integer. e.g: open 0\n";
                continue;
            }

            pin = device->OpenPin(pinId);
        }
        else if (command == L"freq") {
            // set controller frequency
            double freq;

            if (!(linestream >> freq)) {
                std::wcout << L"Expecting float.\n";
                continue;
            }

            device->SetDesiredFrequency(freq);
        }
        else if (command == L"start") {
            if (!pin) {
                std::wcout << L"No open pin\n";
                continue;
            }

            pin->Start();
        }
        else if (command == L"stop") {
            if (!pin) {
                std::wcout << L"No open pin\n";
                continue;
            }
            else if (!pin->IsStarted) {
                std::wcout << L"Pin not started\n";
                continue;
            } 

            pin->Stop();
        } 
        else if (command == L"dutycycle") {
            double duty;

            if (!(linestream >> duty)) {
                std::wcout << L"Expecting float.\n";
                continue;
            } else if (!pin) {
                std::wcout << L"No open pin\n";
                continue;
            } else if ((duty < 0.0) || (duty > 100.0)) {
                std::wcout << "Duty cycle must be between 0 and 100\n";
                continue;
            }

            pin->SetActiveDutyCyclePercentage(duty / 100.0);
        }
        else if (command == L"polarity") {
            if (!pin) {
                std::wcout << L"No open pin\n";
                continue;
            }

            if (pin->Polarity == PwmPulsePolarity::ActiveHigh) {
                pin->Polarity = PwmPulsePolarity::ActiveLow;
            }
            else {
                pin->Polarity = PwmPulsePolarity::ActiveHigh;
            }

            std::wcout << L"Polarity is now " << pin->Polarity << L"\n";
        }
        else if (command == L"info") {
            std::wcout << L" ActualFrequency: " << device->ActualFrequency << L"\n";
            std::wcout << L"    MaxFrequency: " << device->MaxFrequency << L"\n";
            std::wcout << L"    MinFrequency: " << device->MinFrequency << L"\n";
            std::wcout << L"        PinCount: " << device->PinCount << L"\n";

            if (pin) {
                std::wcout << L"                 IsStarted: " << pin->IsStarted << L"\n";
                std::wcout << L"                  Polarity: " << pin->Polarity << L"\n";
                std::wcout << L" ActiveDutyCyclePercentage: " << pin->GetActiveDutyCyclePercentage() * 100.0 << L"\n";
            }
        } else if (command.empty()) {
            // ignore
        } else {
            std::wcout << L"Unrecognized command: " << command <<
                L". Type 'help' for command usage.\n";
        }
    }
}

void PrintUsage (PCWSTR name)
{
    wprintf(
        L"pwmTestTool: Command line pwm testing utility\n"
        L"Usage: %s [-list] [FriendlyName]\n"
        L"\n"
        L"  -list          List available PWM controllers and exit.\n"
        L"  FriendlyName   The friendly name of the PWM controller over\n"
        L"                 which you wish to communicate. This parameter is\n"
        L"                 optional and defaults to the first enumerated\n"
        L"                 PWM controller.\n"
        L"\n"
        L"Examples:\n"
        L"  List available PWM controllers and exit:\n"
        L"    %s -list\n"
        L"\n"
        L"  Open connection to PWM1:\n"
        L"    %s PWM1\n",
        name,
        name,
        name);
}

int main (Platform::Array<Platform::String^>^ args)
{
    unsigned int optind = 1;
    if (optind >= args->Length) {
        std::wcerr << L"Missing required command line parameter\n\n";
        PrintUsage(args->get(0)->Data());
        return 1;
    }

    PCWSTR arg = args->get(optind)->Data();
    if (!_wcsicmp(arg, L"-h") || !_wcsicmp(arg, L"/h") ||
        !_wcsicmp(arg, L"-?") || !_wcsicmp(arg, L"/?")) {

        PrintUsage(args->get(0)->Data());
        return 0;
    }

    if (!_wcsicmp(arg, L"-l") || !_wcsicmp(arg, L"-list")) {
        ListPwmControllers();
        return 0;
    }

    String^ friendlyName;
    if (optind < args->Length) {
        friendlyName = args->get(optind++);
    }

    try {
        auto device = MakeDevice(friendlyName);

        std::wcout << L"  Type 'help' for a list of commands\n";
        ShowPrompt(device);
    } catch (const wexception& ex) {
        std::wcerr << L"Error: " << ex.wwhat() << L"\n";
        return 1;
    } catch (Platform::Exception^ ex) {
        std::wcerr << L"Error: " << ex->Message->Data() << L"\n";
        return 1;
    }

    return 0;
}
