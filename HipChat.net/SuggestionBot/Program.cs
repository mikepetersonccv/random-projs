﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HipChat;

namespace SuggestionBot
{
    class Program
    {
        /// <summary>
        /// Mains the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        static void Main( string[] args )
        {
            const int nerdHallRoomId = 222901;
            const int debugRoomId = 284955;
            const int nerdClosetRoomId = 597790;

            const int currentRoomId = nerdClosetRoomId;
            
            // HipChat initialization
            HipChatClient hipChatClient = new HipChatClient( "54f19307e4626c9578d9dcb267ea19", currentRoomId, "SuggestionBot" );
            DateTime startupDateTime = DateTime.Now;
            CommandDatabase commands = new CommandDatabase();
            Nouns _nouns = new Nouns();
            DateTime lastSuggestion = DateTime.MinValue;
            double suggestionMinutes = 61;
            bool keepRunning = true;

            // rate limit is 100 API requests per 5 minutes
            int minWaitTimeSeconds = 5 * 60 / 100;
            DateTime lastGotDateTime = DateTime.UtcNow.Subtract( new TimeSpan( 0, 0, 5 ) );

            while ( keepRunning )
            {
                try
                {
                    //Send suggestion :)
                    if ( ( DateTime.Now - lastSuggestion ).TotalMinutes >= suggestionMinutes )
                    {
                        int randomYear = new Random().Next( 2014, 2018 );
                        int randomResponseType = new Random().Next( 1, 4 );
                        if ( randomResponseType == 1 )
                        {
                            hipChatClient.SendMessage( "Lunch Suggestion: " + _nouns.GetRandomNoun() + " " + _nouns.GetRandomNoun());
                        }
                        else if (randomResponseType == 2)
                        {
                            int randomNounCount = new Random().Next( 3 ) + 1;
                            string nounsList = string.Empty;
                            for ( int i = 0; i <= randomNounCount; i++ )
                            {
                                nounsList += " " + _nouns.GetRandomNoun() + ",";
                            }

                            hipChatClient.SendMessage( "Pizza Topping suggestions: " + nounsList.TrimEnd(',') );
                        }
                        else if ( randomResponseType == 3 )
                        {
                            hipChatClient.SendMessage( "Sammich Suggestion: " + _nouns.GetRandomNoun() + " and " + _nouns.GetRandomNoun() + ", smothered with " + _nouns.GetRandomNoun() );
                        }
                        else 
                        {
                            hipChatClient.SendMessage( "Game Suggestion: " + _nouns.GetRandomNoun() + " " + _nouns.GetRandomNoun() + " Simulator " + randomYear.ToString() );
                        }

                        lastSuggestion = DateTime.Now;
                    }

                    System.Threading.Thread.Sleep( ( minWaitTimeSeconds * 4 ) * 1000 );

                    List<HipChat.Entities.Message> recentMessageList = hipChatClient.ListHistoryAsNativeObjects();
                    recentMessageList = recentMessageList.OrderBy( a => a.Date ).ToList();
                    if ( recentMessageList.Count > 0 )
                    {
                        var lastMessage = recentMessageList.Last();
                        recentMessageList = recentMessageList.Where( a => a.Date > lastGotDateTime ).Where( a => a.From.Name != "SuggestionBot" ).ToList();
                        lastGotDateTime = lastMessage.Date;
                    }

                    #region CommandProcessing

                    foreach ( var messageItem in recentMessageList )
                    {
                        // clean up the message a little
                        messageItem.Text = messageItem.Text.Replace( "\\n", string.Empty ).Trim();

                        Console.ResetColor();
                        Console.WriteLine( messageItem.From.Name + ": " + messageItem.Text );

                        CommandDatabase.Command commandType = commands.GetCommand( messageItem );
                        if ( commandType != CommandDatabase.Command.Unknown )
                        {
                            /* Respond to the RandomAnswerXX class of messagecatagory */
                            string enumName = Enum.GetName( typeof( CommandDatabase.Command ), commandType );

                            switch ( commandType )
                            {
                                /* Bot stuff */
                                case CommandDatabase.Command.BotStats:
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        sb.AppendLine( string.Format( "Running since {0}", startupDateTime.ToString( "F" ) ) );
                                        sb.AppendLine( string.Format( "Messages Sent {0}", hipChatClient.MessageSentCount ) );
                                        sb.AppendLine( string.Format( "Api Calls {0}", hipChatClient.ApiCallCount ) );
                                        hipChatClient.SendMessage( sb.ToString() );
                                    }
                                    break;

                                case CommandDatabase.Command.Help:
                                    {
                                        StringBuilder sb = new StringBuilder();
                                        sb.AppendLine( "<pre>" );
                                        sb.AppendLine( "Help (Shows this)" );
                                        sb.AppendLine( "BotStats (Shows stats)" );
                                        sb.AppendLine( "BotQuit (Stops the bot program)" );
                                        sb.AppendLine( "Suggestions [minutes] (Toggles game name suggestions)" );
                                        sb.AppendLine();
                                        sb.AppendLine( "</pre>" );
                                        hipChatClient.SendMessageHtml( sb.ToString() );
                                        // sleep a little so they come out in the right order
                                        System.Threading.Thread.Sleep( 500 );
                                    }
                                    break;

                                case CommandDatabase.Command.BotQuit:
                                    hipChatClient.SendMessage( "OK, I'll quit now. Goodbye, " + messageItem.From.FirstName );
                                    break;

                                case CommandDatabase.Command.Suggestions:
                                    if ( lastSuggestion == DateTime.MaxValue )
                                    {
                                        lastSuggestion = DateTime.MinValue;
                                        string[] messageParts = messageItem.Text.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );
                                        if ( messageParts.Length == 2 )
                                        {
                                            double minutesParam;
                                            if ( double.TryParse( messageParts[1], out minutesParam ) )
                                            {
                                                suggestionMinutes = minutesParam;
                                            }
                                            else
                                            {
                                                suggestionMinutes = 65;
                                            }
                                        }

                                        hipChatClient.SendMessage( string.Format( "Thank you for asking for game name suggestions! You will get a random game suggestions every {0} minutes", suggestionMinutes ) );
                                    }
                                    else
                                    {
                                        hipChatClient.SendMessage( "I will stop sending you game suggestions now." );
                                        lastSuggestion = DateTime.MaxValue;
                                    }
                                    break;

                                default:
                                    break;
                            }
                        }
                    }

                    #endregion

                }
                catch ( Exception ex )
                {
                    hipChatClient.SendMessage( "Oh man, this happened: " + ex.Message );
                }
            }
        }
    }
}
