using UnityEngine;

public class SupermarktTuer : MonoBehaviour
{
    private Transform Player;
    private Transform TuerRechts;
    private Transform TuerLinks;
    
    private Vector3 startPositionLinks;  
    private Vector3 startPositionRechts;
    private Vector3 zielPositionLinks;
    private Vector3 zielPositionRechts;

    public float oeffnungsAbstand = 5f;
    public float oeffnungsWeite = 2f;
    public float geschwindigkeit = 3f;

    void Start()
    {
        TuerLinks = transform.Find("Door left");
        TuerRechts = transform.Find("Door right");
        Player = GameObject.Find("player").transform;

        

        startPositionLinks = TuerLinks.position;   
        startPositionRechts = TuerRechts.position;

        zielPositionLinks = startPositionLinks + new Vector3(-oeffnungsWeite, 0, 0);
        zielPositionRechts = startPositionRechts + new Vector3(oeffnungsWeite, 0, 0);

        
    }

    void Update()
    {
        if (Player == null || TuerRechts == null) return;

        float abstand = Vector3.Distance(TuerRechts.position, Player.position);
        
        if (abstand < oeffnungsAbstand)
        {
            TuerLinks.position = Vector3.Lerp(TuerLinks.position, zielPositionLinks, Time.deltaTime * geschwindigkeit);
            TuerRechts.position = Vector3.Lerp(TuerRechts.position, zielPositionRechts, Time.deltaTime * geschwindigkeit);
        }
        else
        {
            TuerLinks.position = Vector3.Lerp(TuerLinks.position, startPositionLinks, Time.deltaTime * geschwindigkeit);
            TuerRechts.position = Vector3.Lerp(TuerRechts.position, startPositionRechts, Time.deltaTime * geschwindigkeit);
        }
    }
}