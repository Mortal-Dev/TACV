using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkTestController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayServerStart());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator DelayServerStart()
    {
        yield return new WaitForSeconds(3f);

       // NetworkManager.Instance.StartHost(696, 2, "Game");
       // NetworkManager.Instance.NetworkSceneManager.LoadScene("Game");
    }
}