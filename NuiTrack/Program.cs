using nuitrack;
using nuitrack.issues;
using System;
using Exception = System.Exception;

namespace NuiTrack
{
    class Program
    {
        static private SkeletonTracker _skeletonTracker;
		static private SkeletonData _skeletonData;        

        static void Main(string[] args)
        {
            try
            {
                Nuitrack.Init("C:\\Program Files\\Nuitrack\\nuitrack\\nuitrack\\data\\nuitrack.config");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot initialize Nuitrack.");
                throw exception;
            }

            try
            {
                // Create and setup all required modules
                _skeletonTracker = SkeletonTracker.Create();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot create Nuitrack module.");
                throw exception;
            }
            Nuitrack.Run();
            while (true)
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
                            if (joint.Type == JointType.Head)
                            {
                                Console.WriteLine(joint.Type);
                                float[] m = joint.Orient.Matrix;
                                Console.WriteLine();
                                for (int i = 0; i < 9; i++)
                                {
                                    Console.Write(m[i] + ",");
                                }
                                Console.WriteLine("{0},{1},{2}", joint.Real.X, joint.Real.Y, joint.Real.Z);
                            }
                        }
                    }
                }
            }
            Nuitrack.Release();
        }

        // Event handler for the NewUser event
        private void onUserTrackerNewUser(int id)
        {
            Console.WriteLine("New User {0}", id);
        }

        // Event handler for the LostUser event
        private void onUserTrackerLostUser(int id)
        {
            Console.WriteLine("Lost User {0}", id);
        }

        // Event handler for the SkeletonUpdate event
        private void onSkeletonUpdate(SkeletonData skeletonData)
        {
            _skeletonData = skeletonData;
        }
    }
}

