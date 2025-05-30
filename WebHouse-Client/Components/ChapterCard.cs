﻿using System.Drawing.Drawing2D;
using WebHouse_Client.Logic;
using WebHouse_Client.Networking;

namespace WebHouse_Client.Components;

public class ChapterCard : IComponentCard
{
    public static ChapterCard? SelectedChapterCard;
    
    public Card CardComponent { get; }
    public ChapterCardPile? Pile { get; set; }
    public Panel Panel => CardComponent.Panel;
    public Logic.ChapterCard Card { get; }

    public ChapterCard(Logic.ChapterCard card)
    {
        Card = card;
        
        CardComponent = new Card(5, 10, Color.Black, 2, g =>
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            DrawTitle(g);
            DrawArrow(g);
            DrawNeededColors(g);
            DrawCounter(g);
        });
        
        Panel.Tag = this; //Ermöglicht das zugreifen auf ein bestimmtes ChapterCard Objekt
        Panel.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
                OnClick();
        };
        //new DraggableControl(Panel); //macht die Karte direkt bewegbar so das er DraggableControler nicht bei ersrstellen aufgerufen werden muss
    }

    private void OnClick()
    {
        if (!NetworkManager.Instance.LocalPlayer.IsTurn || GameLogic.TurnState == 2)
            return;
        
        //Überprüfen ob eine EscapeCard ausgewählt ist
        if (EscapeCard.SelectedEscapeCard != null)
        {
            if (Card.IsSpecial && Card.Requirements.Count == 0)
            {
                EscapeCard.SelectedEscapeCard.CardComponent.SetHighlighted(false);
                EscapeCard.SelectedEscapeCard = null;
                return;
            }
            
            if (Pile != null || Card.IsSpecial)
            {
                //Überprüft ob die EscapeCard an die ChapterCard angelegt werden darf
                if (Card.DoesEscapeCardMatch(EscapeCard.SelectedEscapeCard.Card))
                {
                    NetworkManager.Rpc.PlaceEscapeCard(EscapeCard.SelectedEscapeCard.Card, Card.IsSpecial ? -1 : Pile.Index);
                }
                else
                {
                    EscapeCard.SelectedEscapeCard.CardComponent.SetHighlighted(false);
                }
           
                EscapeCard.SelectedEscapeCard = null;
                return;
            }
            
            EscapeCard.SelectedEscapeCard.CardComponent.SetHighlighted(false); 
            EscapeCard.SelectedEscapeCard = null;
        }

        if (Card.IsSpecial)
            return;
        
        //Prüft ob die ChapterCard schon ausgewählt ist
        if (SelectedChapterCard == this)
        {
            //Wenn sie schon ausgewählt ist wird sie abgewählt
            CardComponent.SetHighlighted(false);
            SelectedChapterCard = null;
        }
        else
        {
            //Wenn sie noch nicht ausgewählt ist wird sie ausgewählt
            if (SelectedChapterCard != null)
                SelectedChapterCard.CardComponent.SetHighlighted(false);

            SelectedChapterCard = this;
            CardComponent.SetHighlighted(true);
        }
    }
    
    private void DrawTitle(Graphics g)
    {
        Font font = new Font("Arial", Panel.Width, FontStyle.Bold, GraphicsUnit.Pixel); // größer (vorher 12)
        var ratioSize = g.MeasureString(Card.Chapter.ToString(), font);
        font = new Font("Arial", (int)(Panel.Width * 0.8 * ratioSize.Height / ratioSize.Width), FontStyle.Bold, GraphicsUnit.Pixel);
        SizeF textSize = g.MeasureString(Card.Chapter.ToString(), font);
        PointF textPosition = new PointF((Panel.Width - textSize.Width) / 2, Panel.Height / 8f);
        g.DrawString(Card.Chapter.ToString(), font, Brushes.White, textPosition);
    }

    private void DrawArrow(Graphics g)
    {
        int shaftHeight = Panel.Height / 6;
        int shaftWidth = (int)(Panel.Width * 0.65f); // Pfeilschaft verlängert
        int arrowHeight = Panel.Height / 4;
        int arrowHeadWidth = Panel.Width / 4; // größerer Pfeilkopf
        int startX = 0;
        float centerY = (Panel.Height - arrowHeight) / 2f + Panel.Height / 20f;

        RectangleF shaftRect = new RectangleF(startX, centerY, shaftWidth, shaftHeight);

        using var arrowBackground = new SolidBrush(Color.White);
        using var numberFont = new Font("Arial", shaftHeight, FontStyle.Bold, GraphicsUnit.Pixel);
        using var numberColor = new SolidBrush(Color.Black);

        var path = new GraphicsPath();
        path.StartFigure();
        path.AddLine(shaftRect.Right - 1, centerY + shaftHeight / 2f + arrowHeight / 2f,
            shaftRect.Right + arrowHeadWidth, centerY + shaftHeight / 2f);
        path.AddLine(shaftRect.Right + arrowHeadWidth, centerY + shaftHeight / 2f,
            shaftRect.Right - 1, centerY + shaftHeight / 2f - arrowHeight / 2f);
        path.AddLine(shaftRect.Right - 1, centerY + shaftHeight / 2f - arrowHeight / 2f,
            shaftRect.Right - 1, centerY + shaftHeight / 2f + arrowHeight / 2f);
        path.CloseFigure();

        g.FillPath(arrowBackground, path);
        g.FillRectangle(arrowBackground, shaftRect);

        string text = Card.Steps.ToString();
        SizeF textSize = g.MeasureString(text, numberFont);
        float textX = shaftRect.Left + (shaftRect.Width - textSize.Width) / 2;
        float textY = shaftRect.Top + (shaftRect.Height - textSize.Height) / 2;
        g.DrawString(text, numberFont, numberColor, textX, textY);
    }
    
    private void DrawNeededColors(Graphics g)
    {
        int dotWidth = Panel.Width / 8;
        int dotHeight = Panel.Height / 8;
        int cornerRadius = Math.Min(dotWidth, dotHeight) / 4;
        int space = Panel.Width / 50; // größerer Abstand zwischen Farbkästen
        int totalWidth = Card.Requirements.Count * dotWidth + (Card.Requirements.Count - 1) * space;
        int startX = (Panel.Width - totalWidth) / 2;
        int y = Panel.Height - dotHeight - Panel.Height / 30;

        for (int i = 0; i < Card.Requirements.Count; i++)
        {
            Color color = Card.Requirements[i].GetColor();
            using var brush = new SolidBrush(color);
            Rectangle rect = new Rectangle(startX + i * (dotWidth + space), y, dotWidth, dotHeight);
            using var path = RoundedRectangle(rect, cornerRadius);
            g.FillPath(brush, path);
        }
    }

    private GraphicsPath RoundedRectangle(Rectangle rect, int cornerRadius)
    {
        int diameter = cornerRadius * 2;
        GraphicsPath path = new GraphicsPath();
        path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
    private void DrawCounter(Graphics g)
    {
        if(Card.Counter > 0)
        {
            using var font = new Font("Arial", 10, FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
        
            string text = $"#{Card.Counter}";
            SizeF textSize = g.MeasureString(text, font);
            float padding = 8; //Abstand zum Rand
            float x = Panel.Width - textSize.Width - padding;
            float y = padding;
            g.DrawString(text, font, brush, x, y);
        }
    }
}
