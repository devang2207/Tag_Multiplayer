using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTagBehaviour : MonoBehaviour, IPunObservable, IInRoomCallbacks
{
    [SerializeField] Material demonMaterial;
    [SerializeField] Material normalMaterial;
    bool canTagg = true;

    MeshRenderer meshRenderer;
    public bool _isTagged = false;

    private PhotonView _photonView;
    [SerializeField] private PhotonView _gameManagerView;
    [SerializeField] private float canTagCooldown = 100;

    private void Start()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        _photonView = GetComponent<PhotonView>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (_isTagged && other.CompareTag("Player"))
        {
            PhotonView otherPhotonView = other.GetComponent<PhotonView>();
            if (otherPhotonView != null)
            {
                _photonView.RPC("GiveDemonship", RpcTarget.AllBuffered, otherPhotonView.ViewID);
            }
        }
    }

    [PunRPC]
    public void TakeDemonShip()
    {
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        PhotonView pmPhotonView = playerMovement.GetComponent<PhotonView>();
        pmPhotonView.RPC("ConvertIntoDemon", RpcTarget.AllBuffered);
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshRenderer.material = demonMaterial;
        this.gameObject.tag = "It";
        _isTagged = true;
    }
    [PunRPC]
    public void GiveDemonship(int otherViewID)
    {
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        PhotonView pmPhotonView = playerMovement.GetComponent<PhotonView>();
        pmPhotonView.RPC("ConvertIntoPlayerAgain", RpcTarget.AllBuffered);



        PhotonView otherPhotonView = PhotonView.Find(otherViewID);
        PlayerMovement otherPlayerMovement = otherPhotonView.GetComponent<PlayerMovement>();
        otherPlayerMovement.GetComponent<PhotonView>().RPC("ConvertIntoDemon", RpcTarget.AllBuffered);
        if (otherPhotonView == null) return;

        PlayerTagBehaviour otherPlayerTagBehaviour = otherPhotonView.GetComponent<PlayerTagBehaviour>();
        if (otherPlayerTagBehaviour != null)
        {
            // Set the new demon
            if (otherPlayerTagBehaviour.meshRenderer != null)
            {
                otherPlayerTagBehaviour.meshRenderer.material = demonMaterial;
            }
            otherPlayerTagBehaviour.gameObject.tag = "It";
            otherPlayerTagBehaviour._isTagged = true;

            // Notify PlayerSpawner to respawn players
            PlayerSpawner playerSpawner = FindObjectOfType<PlayerSpawner>();
            playerSpawner.RespawnAllPlayers(otherPhotonView.ViewID);
        }

        // Reset the current demon
        if (meshRenderer != null)
        {
            meshRenderer.material = normalMaterial;
        }
        this.gameObject.tag = "Player";
        _isTagged = false;
        StartCoroutine(UpdateCanTagg());
    }
    IEnumerator UpdateCanTagg()
    {
        canTagg = false;
        yield return new WaitForSeconds(canTagCooldown);
        canTagg = true;
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(canTagg);
        }
        if (stream.IsReading)
        {
            canTagg = (bool)stream.ReceiveNext();
        }
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        throw new System.NotImplementedException();
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        PhotonView otherPlayerPhotonView = PhotonView.Find(otherPlayer.ActorNumber);
        if (otherPlayerPhotonView.GetComponent<PlayerTagBehaviour>()._isTagged)
        {
            // Get all players in the room
            Player[] allPlayers = PhotonNetwork.PlayerList;

            // Filter out the leaving player
            List<Player> remainingPlayers = new List<Player>(allPlayers);
            remainingPlayers.Remove(otherPlayer);

            // Check if there are any players left
            if (remainingPlayers.Count > 0)
            {
                // Select a random player from the remaining players
                int randomIndex = Random.Range(0, remainingPlayers.Count);
                Player newTaggedPlayer = remainingPlayers[randomIndex];

                // Find the PhotonView of the new tagged player
                PhotonView newPlayerPhotonView = PhotonView.Find(newTaggedPlayer.ActorNumber);

                // Call RPC to assign "demonship" to the new player
                newPlayerPhotonView.RPC("TakeDemonShip", RpcTarget.AllBuffered);

                // Update the _isTagged variable locally (optional, if needed immediately)
                newPlayerPhotonView.GetComponent<PlayerTagBehaviour>()._isTagged = true;
            }
        }
    }

    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        throw new System.NotImplementedException();
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        throw new System.NotImplementedException();
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        throw new System.NotImplementedException();
    }
}