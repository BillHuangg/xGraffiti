﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Linq;
using System.Text;

namespace xGraffiti
{
    internal struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    internal struct Input
    {
        public int Type;
        public MouseInput MouseInput;
    }

    public static class SetCurson
    {
        public const int InputMouse = 0;
        public const int MouseEventMove = 0x01;
        public const int MouseEventLeftDown = 0x02;
        public const int MouseEventLeftUp = 0x04;
        public const int MouseEventRightDown = 0x08;
        public const int MouseEventRightUp = 0x10;
        public const int MouseEventAbsolute = 0x8000;
        //Mouse
        private static bool lastLeftDown;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numInputs, Input[] inputs, int size);

        public static void SendMouseInput(int positionX, int positionY, int maxX, int maxY, bool leftDown)
        {
            if (positionX > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("positionX");
            }
            if (positionY > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("positionY");
            }

            Input[] i = new Input[2];
            i[0] = new Input();
            i[0].Type = InputMouse;
            i[0].MouseInput.X = (positionX * 70000) / maxX;
            i[0].MouseInput.Y = (positionY * 70000) / maxY;
            i[0].MouseInput.Flags = MouseEventAbsolute | MouseEventMove;
            if (!lastLeftDown && leftDown)
            {
                i[1] = new Input();
                i[1].Type = InputMouse;
                i[1].MouseInput.Flags = MouseEventLeftDown;
            }
            else if (lastLeftDown && !leftDown)
            {
                i[1] = new Input();
                i[1].Type = InputMouse;
                i[1].MouseInput.Flags = MouseEventLeftUp;
            }
            lastLeftDown = leftDown;

            // send it off
            uint result = SendInput(2, i, Marshal.SizeOf(i[0]));
            if (result == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}
