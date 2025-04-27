using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;
using Unity.Cinemachine;


public class Player : MonoBehaviourPunCallbacks, IPunObservable // IPunObservable은 변수 동기화를 위함
{
    public Rigidbody2D rigid;
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public PhotonView photonV;
    public TextMeshProUGUI NickNameText;
    public Image HealthImage;

    private bool isGround;
    private Vector3 curPos; // 다른 pc로부터 전달받은 플레이어의 위치

    private void Awake()
    {
        // 닉네임
        NickNameText.text = photonV.IsMine ? PhotonNetwork.NickName : photonV.Owner.NickName;
        NickNameText.color = photonV.IsMine ? Color.green : Color.red;

        if(photonV.IsMine)
        {
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;
        }
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (photonV.IsMine)
        {
            float axis = Input.GetAxisRaw("Horizontal");
            rigid.linearVelocity = new Vector2(4 * axis, rigid.linearVelocity.y);

            if (axis != 0)
            {
                animator.SetBool("walk", true);
                photonV.RPC("FlipXRPC", RpcTarget.AllBuffered, axis); // 재접속시 flipX를 동기화해주기 위해서 AllBuffered
            }
            else
                animator.SetBool("walk", false);

            // 점프를 위한 바닥체크
            isGround = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, 1 << LayerMask.NameToLayer("Ground"));
            animator.SetBool("jump", !isGround);
            if (Input.GetKeyDown(KeyCode.UpArrow) && isGround)
                photonV.RPC("JumpRPC", RpcTarget.All);

            // 스페이스 입력시 총알 발사
            if(Input.GetKeyDown(KeyCode.Space))
            {
                PhotonNetwork.Instantiate("Prefabs/Bullet", transform.position + new Vector3(spriteRenderer.flipX ? -0.4f : 0.4f, -0.11f, 0), Quaternion.identity)
                    .GetComponent<PhotonView>().RPC("DirRPC", RpcTarget.All, spriteRenderer.flipX ? -1 : 1);
                animator.SetTrigger("shot");
            }
        } // IsMine이 아닌 것들은 부드럽게 위치 동기화
        else if((transform.position - curPos).sqrMagnitude >= 100)
        {
            transform.position = curPos;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);
        }
    }

    [PunRPC]
    void FlipXRPC(float axis)
    {
        spriteRenderer.flipX = (axis == -1);
    }

    [PunRPC]
    void JumpRPC()
    {
        rigid.linearVelocity = Vector2.zero;
        rigid.AddForce(Vector2.up * 700);
    }

    public void Hit()
    {
        HealthImage.fillAmount -= 0.1f;
        if(HealthImage.fillAmount <= 0)
        {
            GameObject.Find("Canvas").transform.Find("RespawnPanel").gameObject.SetActive(true);
            photonV.RPC("DestroyRPC", RpcTarget.AllBuffered); // AllBuffered로 해야 제대로 사라져 복제버그가 안 생긴다
        }
    }

    // DestroyRPC는 플레이어든 총알이든 RPCTarget.AllBuffered로 해야 버그가 안 생긴다
    [PunRPC]
    void DestroyRPC()
    {
        Destroy(gameObject);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(HealthImage.fillAmount);
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext();
            HealthImage.fillAmount = (float)stream.ReceiveNext();
        }
    }
}