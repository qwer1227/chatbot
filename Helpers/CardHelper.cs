using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;

namespace GreatWall.Helpers
{
    public static class CardHelper
    {
        //Create Hero card & return
        public static Attachment GetHeroCard(string strTitle, string strSubTitle, string strImage, 
                                             string strButtonText, string strButtonValue)
        {
            //Create image object
            List<CardImage> images = new List<CardImage>();
            images.Add(new CardImage() { Url = strImage });

            //Create Button 
            List<CardAction> buttons = new List<CardAction>();
            buttons.Add(new CardAction() { Title = strButtonText, Value = strButtonValue,
                                           Type = ActionTypes.ImBack });

            HeroCard card = new HeroCard()
            {
                Title = strTitle,
                Subtitle = strSubTitle,
                Images = images,
                Buttons = buttons
            };

            return card.ToAttachment();
        }

        //Create Thumbnail card & return
        public static Attachment GetThumbnailCard(string strTitle, string strSubTitle, string strImage,
                                             string strButtonText, string strButtonValue)
        {
            //Create image object
            List<CardImage> images = new List<CardImage>();
            images.Add(new CardImage() { Url = strImage });

            //Create Button 
            List<CardAction> buttons = new List<CardAction>();
            buttons.Add(new CardAction() { Title = strButtonText, Value = strButtonValue,
                                           Type = ActionTypes.ImBack });

            ThumbnailCard card = new ThumbnailCard()
            {
                Title = strTitle,
                Subtitle = strSubTitle,
                Images = images,
                Buttons = buttons
            };

            return card.ToAttachment();
        }
    }
}