using WindowsInput;
using WindowsInput.Native;

namespace CommandLib.Commands.RemoteCommandItems
{
    public static class InputHelper
    {
        private static readonly InputSimulator Simulator = new();

        public static void SendInput(RemoteControlObject controlObject, RemoteControlObject lastState)
        {
            
            if (lastState is not null && controlObject.LeftButtonIsPressed != lastState.LeftButtonIsPressed)
            {
                Simulator.Mouse.LeftButtonUp();
                Simulator.Mouse.MoveMouseToPositionOnVirtualDesktop(controlObject.MouseX, controlObject.MouseY);
            }
            else if (controlObject.LeftButtonIsPressed && (lastState is null || !lastState.RightButtonIsPressed))
            {
                Simulator.Mouse.LeftButtonDown();
                Simulator.Mouse.MoveMouseToPositionOnVirtualDesktop(controlObject.MouseX, controlObject.MouseY);
            }
            else
            {
                Simulator.Mouse.MoveMouseToPositionOnVirtualDesktop(controlObject.MouseX, controlObject.MouseY);
                
                switch (controlObject.LeftButtonClickCount)
                {
                    case 1:
                        Simulator.Mouse.LeftButtonClick();
                        break;
                    case 2:
                        Simulator.Mouse.LeftButtonDoubleClick();
                        break;
                }
            }

            if (lastState is not null && controlObject.RightButtonIsPressed != lastState.RightButtonIsPressed)
            {
                Simulator.Mouse.RightButtonUp();
                Simulator.Mouse.MoveMouseToPositionOnVirtualDesktop(controlObject.MouseX, controlObject.MouseY);
            }
            else if (controlObject.RightButtonIsPressed && (lastState is null || !lastState.LeftButtonIsPressed))
            {
                Simulator.Mouse.RightButtonDown();
                Simulator.Mouse.MoveMouseToPositionOnVirtualDesktop(controlObject.MouseX, controlObject.MouseY);
            }
            else
            {
                Simulator.Mouse.MoveMouseToPositionOnVirtualDesktop(controlObject.MouseX, controlObject.MouseY);
                
                switch (controlObject.RightButtonClickCount)
                {
                    case 1:
                        Simulator.Mouse.RightButtonClick();
                        break;
                    case 2:
                        Simulator.Mouse.RightButtonDoubleClick();
                        break;
                }
            }
            
            Simulator.Mouse.VerticalScroll(controlObject.Delta);

            foreach (var (key, state) in controlObject.Keys)
            {
                switch (state)
                {
                    case 1:
                        Simulator.Keyboard.KeyDown((VirtualKeyCode)key);
                        break;
                    case 2:
                    case 3:
                        Simulator.Keyboard.KeyPress((VirtualKeyCode)key);
                        break;
                }
            }
        }
    }
}