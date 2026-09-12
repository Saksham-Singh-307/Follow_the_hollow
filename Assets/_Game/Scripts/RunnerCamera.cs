using UnityEngine;

namespace VoiceRunner
{
    public class RunnerCamera : MonoBehaviour
    {
        public Transform target;
        public float lookAhead = 3.2f;
        public float followSpeed = 8f;
        public float minY = 1.5f;
        public float yFollow = 0.35f;

        float shake;
        Vector3 vel;

        public void Kick(float amount) { shake = Mathf.Max(shake, amount); }

        public void Snap()
        {
            if (target == null) return;
            transform.position = new Vector3(target.position.x + lookAhead, minY, -10f);
        }

        void LateUpdate()
        {
            if (target == null) return;
            float tx = target.position.x + lookAhead;
            float ty = Mathf.Max(minY, minY + (target.position.y - minY) * yFollow);

            Vector3 want = new Vector3(tx, ty, -10f);
            Vector3 pos = Vector3.SmoothDamp(transform.position, want, ref vel, 1f / followSpeed);

            if (shake > 0.001f)
            {
                shake = Mathf.Lerp(shake, 0f, Time.deltaTime * 6f);
                pos += (Vector3)(Random.insideUnitCircle * shake);
            }
            transform.position = pos;
        }
    }

    /// <summary>Cheap parallax bands so the world reads as moving.</summary>
    public class Parallax : MonoBehaviour
    {
        public Transform cam;
        public float factor = 0.35f;
        public float tileWidth = 24f;
        float baseY;

        void Start() { baseY = transform.position.y; }

        void LateUpdate()
        {
            if (cam == null) return;
            float x = cam.position.x * factor;
            float wrapped = Mathf.Repeat(cam.position.x - x, tileWidth);
            transform.position = new Vector3(cam.position.x - wrapped, baseY, transform.position.z);
        }
    }
}
