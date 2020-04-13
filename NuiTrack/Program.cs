using nuitrack;
using nuitrack.issues;
using System;
using Exception = System.Exception;

namespace NuiTrack
{
    class Program
    {
        static private DepthSensor _depthSensor;
        static private ColorSensor _colorSensor;
        static private UserTracker _userTracker;
        static private SkeletonTracker _skeletonTracker;
        static private SkeletonData _skeletonData;

        private static DepthFrame _depthFrame;
        private static IssuesData _issuesData = null;

        private static bool _visualizeColorImage = false;
        private static bool _colorStreamEnabled = false;

        static void Main(string[] args)
        {
            try
            {
                Nuitrack.Init("");
                Console.WriteLine(Nuitrack.GetLicense());
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot initialize Nuitrack.");
                throw exception;
            }

            try
            {
                // Create and setup all required modules
                _depthSensor = DepthSensor.Create();
                _colorSensor = ColorSensor.Create();
                _userTracker = UserTracker.Create();
                _skeletonTracker = SkeletonTracker.Create();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot create Nuitrack module.");
                throw exception;
            }
            // Add event handlers for all modules
            _depthSensor.OnUpdateEvent += onDepthSensorUpdate;
            _colorSensor.OnUpdateEvent += onColorSensorUpdate;
            _userTracker.OnUpdateEvent += onUserTrackerUpdate;
            _userTracker.OnNewUserEvent += onUserTrackerNewUser;
            _userTracker.OnLostUserEvent += onUserTrackerLostUser;
            _skeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;

            // Add an event handler for the IssueUpdate event 
            Nuitrack.onIssueUpdateEvent += onIssueDataUpdate;

            try
            {
                Nuitrack.Run();
                Nuitrack.GetLicense();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot start Nuitrack.");
                throw exception;
            }
            bool a = true;
            while (a)
            {
                // Update Nuitrack data. Data will be synchronized with skeleton time stamps.
                try
                {
                    Nuitrack.Update(_skeletonTracker);
                }
                catch (LicenseNotAcquiredException exception)
                {
                    Console.WriteLine("LicenseNotAcquired exception. Exception: {0}", exception);
                    throw exception;
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Nuitrack update failed. Exception: ", exception);
                }
                if (_skeletonData != null)
                {
                    const int jointSize = 10;
                    foreach (var skeleton in _skeletonData.Skeletons)
                    {
                        foreach (var joint in skeleton.Joints)
                        {
                            if (joint.Type == JointType.LeftHand)
                            {
                                float[] m = joint.Orient.Matrix;
                                Console.WriteLine();
                                for (int i = 0; i < 9; i++)
                                {
                                    Console.Write(m[i] + ",");
                                }
                                Console.WriteLine();
                                Console.WriteLine("{0},{1},{2}", (int)joint.Real.X, (int)joint.Real.Y, (int)joint.Real.Z);
                            }
                        }
                    }
                }
                Console.Read();
            }

            Nuitrack.Release();
            Console.ReadLine();
        }

        // Event handler for the UserTrackerUpdate event
        private static void onUserTrackerUpdate(UserFrame userFrame)
        {
            if (_visualizeColorImage && _colorStreamEnabled)
                return;
            if (_depthFrame == null)
                return;

            const int MAX_LABELS = 7;
            bool[] labelIssueState = new bool[MAX_LABELS];
            for (UInt16 label = 0; label < MAX_LABELS; ++label)
            {
                labelIssueState[label] = false;
                if (_issuesData != null)
                {
                    FrameBorderIssue frameBorderIssue = _issuesData.GetUserIssue<FrameBorderIssue>(label);
                    labelIssueState[label] = (frameBorderIssue != null);
                }
            }

            //float wStep = (float)_bitmap.Width / _depthFrame.Cols;
            //float hStep = (float)_bitmap.Height / _depthFrame.Rows;

            //float nextVerticalBorder = hStep;

            //Byte[] dataDepth = _depthFrame.Data;
            //Byte[] dataUser = userFrame.Data;
            //int dataPtr = 0;
            //int bitmapPtr = 0;
            //const int elemSizeInBytes = 2;
            //for (int i = 0; i < _bitmap.Height; ++i)
            //{
            //    if (i == (int)nextVerticalBorder)
            //    {
            //        dataPtr += _depthFrame.Cols * elemSizeInBytes;
            //        nextVerticalBorder += hStep;
            //    }

            //    int offset = 0;
            //    int argb = 0;
            //    int label = dataUser[dataPtr] | dataUser[dataPtr + 1] << 8;
            //    int depth = Math.Min(255, (dataDepth[dataPtr] | dataDepth[dataPtr + 1] << 8) / 32);
            //    float nextHorizontalBorder = wStep;
            //    for (int j = 0; j < _bitmap.Width; ++j)
            //    {
            //        if (j == (int)nextHorizontalBorder)
            //        {
            //            offset += elemSizeInBytes;
            //            label = dataUser[dataPtr + offset] | dataUser[dataPtr + offset + 1] << 8;
            //            if (label == 0)
            //                depth = Math.Min(255, (dataDepth[dataPtr + offset] | dataDepth[dataPtr + offset + 1] << 8) / 32);
            //            nextHorizontalBorder += wStep;
            //        }

            //        if (label > 0)
            //        {
            //            int user = label * 40;
            //            if (!labelIssueState[label])
            //                user += 40;
            //            argb = 0 | (user << 8) | (0 << 16) | (0xFF << 24);
            //        }
            //        else
            //        {
            //            argb = depth | (depth << 8) | (depth << 16) | (0xFF << 24);
            //        }

            //        _bitmap.Bits[bitmapPtr++] = argb;
            //    }
            //}
        }

        // Event handler for the NewUser event
        private static void onUserTrackerNewUser(int id)
        {
            Console.WriteLine("New User {0}", id);
        }

        // Event handler for the LostUser event
        private static void onUserTrackerLostUser(int id)
        {
            Console.WriteLine("Lost User {0}", id);
        }

        // Event handler for the SkeletonUpdate event
        private static void onSkeletonUpdate(SkeletonData skeletonData)
        {
            _skeletonData = skeletonData;
        }

        private static void onIssueDataUpdate(IssuesData issuesData)
        {
            _issuesData = issuesData;
        }

        // Event handler for the DepthSensorUpdate event
        private static void onDepthSensorUpdate(DepthFrame depthFrame)
        {
            _depthFrame = depthFrame;
        }

        // Event handler for the ColorSensorUpdate event
        private static void onColorSensorUpdate(ColorFrame colorFrame)
        {
            if (!_visualizeColorImage)
                return;

            _colorStreamEnabled = true;

            //float wStep = (float)_bitmap.Width / colorFrame.Cols;
            //float hStep = (float)_bitmap.Height / colorFrame.Rows;

            //float nextVerticalBorder = hStep;

            //Byte[] data = colorFrame.Data;
            //int colorPtr = 0;
            //int bitmapPtr = 0;
            //const int elemSizeInBytes = 3;

            //for (int i = 0; i < _bitmap.Height; ++i)
            //{
            //    if (i == (int)nextVerticalBorder)
            //    {
            //        colorPtr += colorFrame.Cols * elemSizeInBytes;
            //        nextVerticalBorder += hStep;
            //    }

            //    int offset = 0;
            //    int argb = data[colorPtr]
            //        | (data[colorPtr + 1] << 8)
            //        | (data[colorPtr + 2] << 16)
            //        | (0xFF << 24);
            //    float nextHorizontalBorder = wStep;
            //    for (int j = 0; j < _bitmap.Width; ++j)
            //    {
            //        if (j == (int)nextHorizontalBorder)
            //        {
            //            offset += elemSizeInBytes;
            //            argb = data[colorPtr + offset]
            //                | (data[colorPtr + offset + 1] << 8)
            //                | (data[colorPtr + offset + 2] << 16)
            //                | (0xFF << 24);
            //            nextHorizontalBorder += wStep;
            //        }

            //        _bitmap.Bits[bitmapPtr++] = argb;
            //    }
            //}
        }
    }
}