using UnityEngine;
using UnityEngine.UI;
public class AvatatUnit : MonoBehaviour
{
    [SerializeField] Image AvatarImage;
    [SerializeField] Image AvaratFrameImage;

    public void SetData(Sprite avatarImg, Sprite avatarFrameImg)
    {
        AvatarImage.sprite = avatarImg;
        AvaratFrameImage.sprite = avatarFrameImg;

        AvaratFrameImage.enabled = avatarFrameImg != null;
    }
}
