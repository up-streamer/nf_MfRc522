using System;
using System.Diagnostics;
using System.Threading;
using System.Collections;
//using Bauland.Others;
using Driver.MfRc522;
using Driver.MfRc522.Constants;
using nanoFramework.Hardware.Esp32;



// using Bauland.Others.Constants.MfRc522;
//using GHIElectronics.TinyCLR.Devices.Gpio;
//using GHIElectronics.TinyCLR.Pins;

namespace testRC522
{
    static class Program
    {
        private static MfRc522 _mfRc522;
        static void Main()
        {
            _mfRc522 = new MfRc522("SPI1", 4, 5);

            Debug.WriteLine("###############################################");
            Debug.WriteLine("# SPI1");
            Debug.WriteLine("###############################################");
            Debug.WriteLine($"SPI1_CLOCK: " + Configuration.GetFunctionPin(DeviceFunction.SPI1_CLOCK));
            Debug.WriteLine($"SPI1_MISO: " + Configuration.GetFunctionPin(DeviceFunction.SPI1_MISO));
            Debug.WriteLine($"SPI1_MOSI: " + Configuration.GetFunctionPin(DeviceFunction.SPI1_MOSI));

            Debug.WriteLine("");  
            Debug.WriteLine($"Version: 0x{_mfRc522.GetVersion():X}");
            Debug.WriteLine("");

            //_mfRc522.Test(); //Basic test to troubleshoot comms issues.

            byte[] defaultKey = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };

            byte writeSector = 0x3;

            ArrayList writeData = new ArrayList
            {
                "Test1 6789012345",
                " Test2 789012345",
                "  Test3 89012345"
            };

            //for Read test, uncomment below and comment WriteSector()
            //InfiniteLoop(defaultKey);

            //for Write test, uncomment below and comment InfiniteLoop()
            WriteSector(writeData, writeSector, defaultKey); 
        }


        private static void InfiniteLoop(byte[] key)
        {
            byte[] bufferAtqa = new byte[2];
            byte[] defaultKey = key;

            while (true)
            {
                if (_mfRc522.IsNewCardPresent(bufferAtqa))
                {
                   Debug.WriteLine("Card detected...");
                   Debug.WriteLine($"ATQA: 0x{bufferAtqa[1]:X2},0x{bufferAtqa[0]:X2}");

                    var uid = _mfRc522.PiccReadCardSerial();
                    if (uid != null)
                    {
                        DisplayUid(uid);
                        try
                        {
                            byte pageOrSector = (byte)(uid.GetPiccType() == PiccType.Mifare1K ? 16 : 4);
                            for (byte i = 0; i < pageOrSector; i++)
                            {
                               Debug.WriteLine($"{i}:");
                                var buffer = _mfRc522.GetSector(uid, i, defaultKey /*, PiccCommand.AuthenticateKeyA*/);
                                if (uid.GetPiccType() == PiccType.Mifare1K)
                                {
                                    var c = _mfRc522.GetAccessRights(buffer);
                                    Display1kBuffer(buffer, c);
                                }
                                else if (uid.GetPiccType() == PiccType.MifareUltralight)
                                {
                                    DisplayUltralightBuffer(buffer);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                           Debug.WriteLine(ex.Message);
                        }

                        _mfRc522.Halt();
                        _mfRc522.StopCrypto();
                    }
                }

                Thread.Sleep(1000);
            }
        }

        private static void DisplaySector(byte sector, Uid uid, byte[] key)
        {
            var buffer = _mfRc522.GetSector(uid, sector, key);
            if (uid.GetPiccType() == PiccType.Mifare1K)
            {
                var c = _mfRc522.GetAccessRights(buffer);
                Display1kBuffer(buffer, c);
            }
            else if (uid.GetPiccType() == PiccType.MifareUltralight)
            {
                DisplayUltralightBuffer(buffer);

            }
        }

        private static void WriteSector(ArrayList data, byte sector, byte[] key)
        {
            bool tryWrite = true;
            byte[] bufferAtqa = new byte[2];

            while (tryWrite)
            {
                if (_mfRc522.IsNewCardPresent(bufferAtqa))
                {

                    Debug.WriteLine("Card detected...");
                    Debug.WriteLine($"ATQA: 0x{bufferAtqa[1]:X2},0x{bufferAtqa[0]:X2}");

                    var uid = _mfRc522.PiccReadCardSerial();
                    if (uid != null)
                    {
                        DisplayUid(uid);
                        try
                        {
                            Debug.WriteLine($"Sector {sector} content was...");
                            DisplaySector(sector, uid, key);

                            _mfRc522.PutSector(data, uid, sector, key);

                            Debug.WriteLine("");
                            Debug.WriteLine($"Now Sector {sector} content is...");
                            DisplaySector(sector, uid, key);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                    tryWrite = false;
                }
            }
            _mfRc522.Halt();
            _mfRc522.StopCrypto();
        }

        private static void DisplayUltralightBuffer(byte[][] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var line = "";
                for (int j = 0; j < buffer[i].Length; j++)
                {
                    line += $"{buffer[i][j]:X2} ";
                }
               Debug.WriteLine(line);
            }
        }

        private static void Display1kBuffer(byte[][] buffer, byte[] accessRights)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                var line = "";
                var text = "";
                for (int j = 0; j < buffer[i].Length; j++)
                {
                    line += $"{buffer[i][j]:X2} ";
                    text += Convert.ToChar(buffer[i][j]);
                }
                line += $"[{(accessRights[0] >> i) & 0x01} {(accessRights[1] >> i) & 0x01} {(accessRights[2] >> i) & 0x01}] ";
                line += text;
               Debug.WriteLine(line);
            }
        }

        private static void DisplayUid(Uid uid)
        {
            string msg = "Uid of card is: ";
            for (int i = 0; i < (int)uid.UidType; i++)
            {
                msg += $"{uid.UidBytes[i]:X2} ";
            }
            msg += $"SAK: {uid.Sak:X2}";
           Debug.WriteLine(msg);
            switch (uid.GetPiccType())
            {
                case PiccType.Mifare1K:
                   Debug.WriteLine("PICC type: MIFARE 1K");
                    break;
                case PiccType.MifareUltralight:
                   Debug.WriteLine("PICC type: MIFARE Ultralight");
                    break;
                default:
                   Debug.WriteLine("PICC type: Unknown");
                    break;
            }
        }
    }
}