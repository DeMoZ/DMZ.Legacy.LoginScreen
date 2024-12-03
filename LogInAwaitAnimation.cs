using System.Collections;
using UnityEngine;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInAwaitAnimation : MonoBehaviour
    {
        [SerializeField] private float _speed = 100;
        private void OnEnable()
        {
            StartCoroutine(Rotate());
        }
        
        private IEnumerator Rotate()
        {
            while (true)
            {
                transform.Rotate(0, 0, 1 * Time.deltaTime * _speed);
                yield return null;
            }
        }
    }
}