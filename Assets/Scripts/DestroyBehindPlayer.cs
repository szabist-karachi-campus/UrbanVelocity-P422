using UnityEngine;
public class DestroyBehindPlayer : MonoBehaviour {
    Transform player;
    void Start() { 
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if(p) player = p.transform;
    }
    void Update() {
        if(player && transform.position.z < player.position.z - 20f) Destroy(gameObject);
    }
}