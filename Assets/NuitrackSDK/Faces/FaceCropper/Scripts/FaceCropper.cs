using UnityEngine;

using NuitrackSDK.Frame;
using UnityEngine.Events;


namespace NuitrackSDK.Face
{
    [AddComponentMenu("NuitrackSDK/Face/Face Cropper/Face Cropper")]
    public class FaceCropper : TrackedUser
    {
        [System.Serializable]
        public class TextureEvent : UnityEvent<Texture> { }

        [SerializeField, NuitrackSDKInspector, Range(0, 5)]
        float loseTime = 0.25f;

        float t = 0;

        [Header("Visualisation")]
        [SerializeField, NuitrackSDKInspector, Range(0, 1)]
        float margin = 0.1f;

        [SerializeField, NuitrackSDKInspector]
        Texture2D noUserImage;

        [SerializeField, NuitrackSDKInspector]
        bool smoothMove = true;

        [SerializeField, NuitrackSDKInspector, Range(0.1f, 24f)]
        float smoothSpeed = 4f;

        [Header("Advanced")]
        [SerializeField, NuitrackSDKInspector]
        bool useGPUCrop = true;

        Rect faceRect;
        Rect targerRect;

        [Header("Output")]
        [SerializeField, NuitrackSDKInspector]
        TextureEvent onFrameUpdate;

        /// <summary>
        /// Cropped face texture (may be null)
        /// </summary>
        public Texture2D CroppedFaceTexture
        {
            get; private set;
        }

        void ResetFrame()
        {
            onFrameUpdate.Invoke(noUserImage);
            faceRect = default;
        }  

        void OnDestroy()
        {
            if (CroppedFaceTexture != null)
                Destroy(CroppedFaceTexture);
        }

        void Update()
        {
            UserData userData = ControllerUser;

            if (userData == null || userData.Face == null)
            {
                if (t > loseTime)
                    ResetFrame();
                else if (t <= loseTime)
                    t += Time.deltaTime;

                return;
            }
            else
            {
                Process(userData);
                t = 0;
            }
        }

        void Process(UserData userData)
        {
            Texture2D frame = NuitrackManager.ColorFrame.ToTexture2D();

            if (frame == null)
            {
                ResetFrame();
                return;
            }

            targerRect = userData.Face.ScreenRect(frame.width, frame.height);

            if (faceRect.Equals(default))
                faceRect = targerRect;

            Vector2 deltaSize = targerRect.size * margin;

            targerRect.position -= deltaSize * 0.5f;
            targerRect.size += deltaSize;

            faceRect.xMin = Mathf.Clamp(faceRect.xMin, 0, frame.width);
            faceRect.xMax = Mathf.Clamp(faceRect.xMax, 0, frame.width);

            faceRect.yMin = Mathf.Clamp(faceRect.yMin, 0, frame.height);
            faceRect.yMax = Mathf.Clamp(faceRect.yMax, 0, frame.height);

            if (CroppedFaceTexture != null)
                Destroy(CroppedFaceTexture);

            CroppedFaceTexture = new Texture2D((int)faceRect.width, (int)faceRect.height, TextureFormat.RGBA32, false);

            if (useGPUCrop)
                Graphics.CopyTexture(frame, 0, 0, (int)faceRect.x, (int)faceRect.y, (int)faceRect.width, (int)faceRect.height, CroppedFaceTexture, 0, 0, 0, 0);
            else
            {
                Color[] pixels = frame.GetPixels((int)faceRect.x, (int)faceRect.y, (int)faceRect.width, (int)faceRect.height);
                CroppedFaceTexture.SetPixels(pixels);
                CroppedFaceTexture.Apply();
            }

            onFrameUpdate.Invoke(CroppedFaceTexture);

            if (smoothMove)
            {
                faceRect.position = Vector2.Lerp(faceRect.position, targerRect.position, Time.deltaTime * smoothSpeed);
                faceRect.size = Vector2.Lerp(faceRect.size, targerRect.size, Time.deltaTime * smoothSpeed);
            }
            else
                faceRect = targerRect;
        }
    }
}