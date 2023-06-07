using ArucoUnity.Plugin;
using UnityEngine;

namespace ArucoUnity.Objects.Trackers
{
    public class ArucoCharucoBoardTrackerInv : ArucoCharucoBoardTracker
    {
        public override void UpdateTransforms(int cameraId, Aruco.Dictionary dictionary)
        {
            base.UpdateTransforms(cameraId, dictionary);

            foreach (var arucoCharucoBoard in arucoTracker.GetArucoObjects<ArucoCharucoBoard>(dictionary))
            {
                if (arucoCharucoBoard.Rvec != null)
                {
                    Matrix4x4 boardMat = new Matrix4x4();
                    Cv.Mat rotationVec = new Cv.Mat();
                    Cv.Rodrigues(arucoCharucoBoard.Rvec, out rotationVec);
                    //Debug.Log(rotationVec.Channels());

                    //boardMat.SetTRS(arucoCharucoBoard.Tvec.ToPosition(), arucoCharucoBoard.Rvec.ToRotation(), Vector3.one);
                    //Matrix4x4 invMat = boardMat.inverse;

                    //arucoCameraDisplay.PlaceArucoObject(arucoCharucoBoard.transform, cameraId, invMat.GetPosition(),
                    //  invMat.rotation);
                }
            }
        }
    }
}