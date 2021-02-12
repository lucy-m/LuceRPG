using LuceRPG.Game.WorldObjects;
using UnityEngine;
using UnityEngine.UI;

public class UnitNameController : MonoBehaviour
{
    public Text Text;

    private UniversalController _following;
    private Vector3 _offset;

    public void SetFollow(string name, UniversalController uc)
    {
        Text.text = name;
        _following = uc;
        _offset = transform.position - uc.transform.position;
    }

    private void Update()
    {
        if (_following == null)
        {
            Destroy(gameObject);
        }
        else
        {
            var ucLocation = _following.transform.position;
            transform.position = ucLocation + _offset;
        }
    }
}
