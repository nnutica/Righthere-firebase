using System;

namespace Firebasemauiapp.Model;

public class PostItColorOption
{
    public string ColorName { get; set; }
    public string PostItImage { get; set; }
    public string TextColor { get; set; }
    public string CircleColor { get; set; }

    public static List<PostItColorOption> GetAllColors()
    {
        return new List<PostItColorOption>
        {
            new PostItColorOption
            {
                ColorName = "Yellow",
                PostItImage = "postit_yellow.png",
                TextColor = "#8FB78F",
                CircleColor = "#FFE66D"
            },
            new PostItColorOption
            {
                ColorName = "Pink",
                PostItImage = "postit_pink.png",
                TextColor = "#5A2A3C",
                CircleColor = "#FFB3D9"
            },
            new PostItColorOption
            {
                ColorName = "Cyan",
                PostItImage = "postit_cyan.png",
                TextColor = "#1F3A3A",
                CircleColor = "#70D6E8"
            },
            new PostItColorOption
            {
                ColorName = "Gray",
                PostItImage = "postit_grey.png",
                TextColor = "#2E3440",
                CircleColor = "#B0C4D8"
            },
            new PostItColorOption
            {
                ColorName = "Green",
                PostItImage = "postit_green.png",
                TextColor = "#2E4F3E",
                CircleColor = "#A8D5BA"
            }
        };
    }
}
