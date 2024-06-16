using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Unity.Services.Matchmaker.Models.MultiplayAssignment;

public class MetchmakerClient : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject canvas;
    void Start()
    {
        StartAsClient();
    }

    // Update is called once per frame
    //client side code
    public string matchmakerAuth;
    public string queueName;
    bool m_IsMatchmaking = false;
    private string _ticketId;
    string m_LastUsedTicket;
    string matchId;

    async void StartAsClient()
    {
        await UnityServices.InitializeAsync();

       await SigIn();
        StartClient();
    }
    [Serializable]
    public class MatchmakingPlayerData
    {
        public int Skill;
    }

    public void StartClient()
    {

   /*     GameObject currentlySelectedButton = EventSystem.current.currentSelectedGameObject;
        currentlySelectedButton.GetComponent<Button>().interactable = false;*/
        print("start client is called");
        CreateATicket();
        matchmakerAuth = PlayerId();
        print("matchmakerAuth is " + matchmakerAuth);
    }

    private async void CreateATicket()
    {
        var options = new CreateTicketOptions(queueName);
        var players = new List<Player>
            {
                new Player(PlayerId(),new MatchmakingPlayerData
                {
                    Skill = 100,
                })
            };
        m_IsMatchmaking = true;
        var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
        _ticketId = ticketResponse.Id;

        Debug.Log($"Ticket ID{_ticketId}");
        m_LastUsedTicket = _ticketId;
        PollTicketStatus();
    }

    private async void PollTicketStatus()
    {
        MultiplayAssignment multiplayAssigment = null;
        bool gotAssignment = false;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1f));
            var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(_ticketId);
            if (ticketStatus == null) continue;
            if (ticketStatus.Type == typeof(MultiplayAssignment))
            {
                multiplayAssigment = ticketStatus.Value as MultiplayAssignment;
            }

            switch (multiplayAssigment.Status)
            {
                case StatusOptions.Found:
                    gotAssignment = true;
                    TicketAssigned(multiplayAssigment);
                    break;

                case StatusOptions.InProgress:

                    break;
                case StatusOptions.Failed:


                    gotAssignment = true;
                    print("failed to get ticket status.Error" + multiplayAssigment.Message);
                    break;
                case StatusOptions.Timeout:
                    gotAssignment = true;
                    print("failed to get ticket status due to timeout" + multiplayAssigment.Message);
                    break;
                default:
                    throw new InvalidOperationException();

            }
        } while (!gotAssignment);

    }

    private void TicketAssigned(MultiplayAssignment multiplayAssigment)
    {
        Debug.Log($"Ticket Assigned: {multiplayAssigment.Ip}:{multiplayAssigment.Port}");
        matchId = multiplayAssigment.MatchId;
        print("session name" + multiplayAssigment.MatchId);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(multiplayAssigment.Ip, 7777);

        NetworkManager.Singleton.StartClient();
        canvas.SetActive(false);

        /*   NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(multiplayAssigment.Ip, (ushort)multiplayAssigment.Port);
           NetworkManager.Singleton.StartClient();
   */
    }
    private async Task SigIn()
    {
        //await ClientSigIn("Snake player");
        await ClientSigIn();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async Task ClientSigIn(string serviceProficeName = null)
    {
        if (serviceProficeName != null)
        {

            var initOptions = new InitializationOptions();
            initOptions.SetProfile(serviceProficeName);
            await UnityServices.InitializeAsync(initOptions);
        }
        else
        {
            print("initialized");
            await UnityServices.InitializeAsync();

        }
        Debug.Log($"Signed in Anonymously as +{serviceProficeName} ({PlayerId()})");

    }
    private string PlayerId()
    {
        return AuthenticationService.Instance.PlayerId;
    }
}
