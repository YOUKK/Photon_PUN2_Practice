using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;

public class Bullet : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView PhotonV;

    private int dir;

    void Start()
    {
        Destroy(gameObject, 3.5f);
    }

    void Update()
    {
        transform.Translate(Vector3.right * 7 * Time.deltaTime * dir);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground"))
            PhotonV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        if(!PhotonV.IsMine && collision.CompareTag("Player") && collision.GetComponent<PhotonView>().IsMine) // 느린쪽에 맞춰서 Hit판정
        {
            collision.GetComponent<Player>().Hit();
            PhotonV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void DirRPC(int dir)
    {
        this.dir = dir;
    }

    [PunRPC]
    void DestroyRPC()
    {
        PhotonView.Destroy(gameObject);
        //Destroy(gameObject);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
    }
}
