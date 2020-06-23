using nuitrack;
using nuitrack.issues;
using System;
using System.Collections.Generic;
using Ventuz.OSC;
using Exception = System.Exception;


namespace NuiTrack
{
    class Program
    {
        private static DepthSensor _depthSensor;
        private static UserTracker _userTracker;
        private static SkeletonTracker _skeletonTracker;
        private static SkeletonData _skeletonData;

        private static DepthFrame _depthFrame;
        private static IssuesData _issuesData = null;
        private static int delay = 1000 / 30;

        static void Main(string[] args)
        {//UDP writer for OSC
            UdpWriter udpWrite = new UdpWriter("127.0.0.1", 2080);

            try
            {
                Nuitrack.Init("");
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
                _userTracker = UserTracker.Create();
                _skeletonTracker = SkeletonTracker.Create();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot create Nuitrack module.");
                throw exception;
            }
            // Add event handlers for all modules
            _depthSensor.OnUpdateEvent += OnDepthSensorUpdate;
            _userTracker.OnNewUserEvent += OnUserTrackerNewUser;
            _userTracker.OnLostUserEvent += OnUserTrackerLostUser;
            _skeletonTracker.OnSkeletonUpdateEvent += OnSkeletonUpdate;

            // Add an event handler for the IssueUpdate event 
            Nuitrack.onIssueUpdateEvent += OnIssueDataUpdate;

            try
            {
                Nuitrack.Run();
                Console.WriteLine(DateTime.Now.ToString());
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot start Nuitrack.");
                throw exception;
            }
            bool a = true;
            while (a)
            {
                int start = (int)DateTime.Now.TimeOfDay.TotalMilliseconds;
                // Update Nuitrack data. Data will be synchronized with skeleton time stamps.
                try
                {
                    Nuitrack.Update(_skeletonTracker);
                }
                catch (LicenseNotAcquiredException exception)
                {
                    Console.WriteLine(DateTime.Now.ToString());
                    Console.WriteLine("LicenseNotAcquired exception. Exception: {0}", exception);
                    throw exception;
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Nuitrack update failed. Exception: ", exception);
                }
                if (_skeletonData != null)
                {
                    //Create new bundle for each time skeleton data is refreshed
                    OscBundle bundle = new OscBundle();

                    //const int jointSize = 10;
                    foreach (var skeleton in _skeletonData.Skeletons)
                    {
                        foreach (var joint in skeleton.Joints)
                        {
                            float[] rotationMatrix = joint.Orient.Matrix;

                            //Ignore joints that are not currently used by Nuitrack
                            if (joint.Type == JointType.None || joint.Type == JointType.LeftFingertip || joint.Type == JointType.RightFingertip || joint.Type == JointType.LeftFoot || joint.Type == JointType.RightFoot)
                            {
                                continue;
                            }

                            //Create new message element for joint containing joint type and rotation matrix
                            OscElement jointMessage = new OscElement("/" + joint.Type, joint.Real.X, joint.Real.Y, joint.Real.Z, rotationMatrix[0], rotationMatrix[1], -1 * rotationMatrix[2], rotationMatrix[3], rotationMatrix[4], -1 * rotationMatrix[5], -1 * rotationMatrix[6], -1 * rotationMatrix[7], rotationMatrix[8]);
                            Console.WriteLine(joint.Real.X + " " + joint.Real.Y + " " + joint.Real.Z);
                            bundle.AddElement(jointMessage);
                        }
                        //Send the message bundle with the data
                        udpWrite.Send(bundle);
                        int difference = delay - start - (int) DateTime.Now.TimeOfDay.TotalMilliseconds;
                        System.Threading.Thread.Sleep(delay);
                    }
                }
            }

            Nuitrack.Release();
            Console.ReadLine();
        }

        // Event handler for the NewUser event
        private static void OnUserTrackerNewUser(int id)
        {
            Console.WriteLine("New User {0}", id);
        }

        // Event handler for the LostUser event
        private static void OnUserTrackerLostUser(int id)
        {
            Console.WriteLine("Lost User {0}", id);
        }

        // Event handler for the SkeletonUpdate event
        private static void OnSkeletonUpdate(SkeletonData skeletonData)
        {
            _skeletonData = skeletonData;
        }

        private static void OnIssueDataUpdate(IssuesData issuesData)
        {
            _issuesData = issuesData;
        }

        // Event handler for the DepthSensorUpdate event
        private static void OnDepthSensorUpdate(DepthFrame depthFrame)
        {
            _depthFrame = depthFrame;
        }
    }
}