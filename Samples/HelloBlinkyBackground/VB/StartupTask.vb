' Copyright (c) Microsoft. All rights reserved.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Net.Http
Imports Windows.ApplicationModel.Background
Imports Windows.Devices.Gpio

' The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

Public NotInheritable Class StartupTask
	Implements IBackgroundTask

    Dim pin As GpioPin
    Dim deferral As BackgroundTaskDeferral
    Dim timer As Windows.System.Threading.ThreadPoolTimer
    'Dim lightOn As Boolean
    Dim value As GpioPinValue = GpioPinValue.High

    Dim _cancelReason = BackgroundTaskCancellationReason.Abort
    Dim _cancelRequested As Boolean = False

    Public Sub Run(taskInstance As IBackgroundTaskInstance) Implements IBackgroundTask.Run

        Debug.WriteLine("Background " + taskInstance.Task.Name + " Starting...")
        AddHandler taskInstance.Canceled, AddressOf OnCanceled

        deferral = taskInstance.GetDeferral()
        pin = GpioController.GetDefault().OpenPin(5)
        pin.SetDriveMode(GpioPinDriveMode.Output)
        ' lightOn = False
        Windows.System.Threading.ThreadPoolTimer.CreatePeriodicTimer(AddressOf Tick, TimeSpan.FromMilliseconds(500))

    End Sub

    Public Sub Tick(timer As Windows.System.Threading.ThreadPoolTimer)

        If (_cancelRequested = False) Then
            value = If(value = GpioPinValue.High, GpioPinValue.Low, GpioPinValue.High)
            pin.Write(value)
        Else
            timer.Cancel()
            ' Indicate that the background task has completed.
            deferral.Complete()
        End If

    End Sub

    Private Sub OnCanceled(sender As IBackgroundTaskInstance, reason As BackgroundTaskCancellationReason)

        _cancelRequested = True
        _cancelReason = reason

        Debug.WriteLine("Background " + sender.Task.Name + " Cancel Requested...")
    End Sub


End Class
