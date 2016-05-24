﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using DevSpace.Common;
using Newtonsoft.Json;
using Microsoft.FSharp.Collections;

namespace DevSpace.Api.Controllers {
	public class JsonStudentCodeBinder : IModelBinder {
		public bool BindModel( HttpActionContext actionContext, ModelBindingContext bindingContext ) {
			HttpContent content = actionContext.Request.Content;
			string json = content.ReadAsStringAsync().Result;
			IStudentCode obj = JsonConvert.DeserializeObject<StudentCode>( json );
			bindingContext.Model = obj;
			return true;
		}
	}

	public class TicketController : ApiController {
		private class AccessCode {
			public string code { get; set; }
			public string[] ticket_ids { get; set; }
			public int quantity_available { get; set; }
		}

		private class EventBriteApiObject {
			public AccessCode access_code { get; set; }
		}

		private FSharp.Database.DataStore<FSharp.Common.StudentCode> _DataStore;
		public TicketController( FSharp.Database.DataStore<FSharp.Common.StudentCode> DataStore ) {
			this._DataStore = DataStore;
		}

		private class MutableStudentCode : IStudentCode {
			public string Code { get; set; }
			public string Email { get; set; }
			public int Id { get; set; }
		};

		[AllowAnonymous]
		public async Task<HttpResponseMessage> Post( [ModelBinder(typeof(JsonStudentCodeBinder))]IStudentCode value ) {
			FSharp.Common.StudentCode NewStudentCode = new FSharp.Common.StudentCode( -1, value.Email, null );

			if( null == NewStudentCode ) return new HttpResponseMessage( HttpStatusCode.BadRequest );

			// Check for .edu email
			if( string.IsNullOrWhiteSpace( NewStudentCode.Email ) )
				return new HttpResponseMessage( HttpStatusCode.BadRequest );

			if( !NewStudentCode.Email.Trim().EndsWith( ".edu", StringComparison.InvariantCultureIgnoreCase ) )
				return new HttpResponseMessage( HttpStatusCode.BadRequest );

			// Check DataStore for existing code
			FSharpList<FSharp.Common.StudentCode> ExistingCodes = await _DataStore.Get( "Email", NewStudentCode.Email );

			//	If exists, resent existing code
			if( ExistingCodes.Length > 0 ) {
				SendEmail( ExistingCodes.Head );
				return new HttpResponseMessage( HttpStatusCode.NoContent );
			}

			// Generate Code
			NewStudentCode = NewStudentCode.WithCode( BitConverter.ToString( Guid.NewGuid().ToByteArray() ).Replace( "-", "" ).Substring( 0, 16 ) );
			while( 1 == ( await _DataStore.Get( "Code", NewStudentCode.Code ) ).Length )
				NewStudentCode = NewStudentCode.WithCode( BitConverter.ToString( Guid.NewGuid().ToByteArray() ).Replace( "-", "" ).Substring( 0, 16 ) );

			// Call EventBrite to create code
#if DEBUG
			string EventBriteApiKey = Environment.GetEnvironmentVariable( "EVENTBRITEAPIKEY", EnvironmentVariableTarget.Machine );
			string EventBriteEventId = Environment.GetEnvironmentVariable( "EVENTBRITEEVENTID", EnvironmentVariableTarget.Machine );
			string EventBriteTicketId = Environment.GetEnvironmentVariable( "EVENTBRITETICKETID", EnvironmentVariableTarget.Machine );
#else
			string EventBriteApiKey = ConfigurationManager.AppSettings["EventBriteApiKey"];
			string EventBriteEventId = ConfigurationManager.AppSettings["EventBriteEventId"];
			string EventBriteTicketId = ConfigurationManager.AppSettings["EventBriteTicketId"];
#endif
			EventBriteApiObject JsonObject = new EventBriteApiObject {
				access_code = new AccessCode {
					code = NewStudentCode.Code,
					ticket_ids = new string[] { EventBriteTicketId },
					quantity_available = 1
				}
			};

			string Uri = string.Format( "https://www.eventbriteapi.com/v3/events/{0}/access_codes/", EventBriteEventId );

			HttpWebRequest EventBriteRequest = HttpWebRequest.Create( Uri ) as HttpWebRequest;
			EventBriteRequest.Headers.Add( "Authorization", string.Format( "Bearer {0}", EventBriteApiKey ) );
			EventBriteRequest.ContentType = "application/json";
			EventBriteRequest.Accept = "application/json";
			EventBriteRequest.Method = "POST";

			byte[] RequestBytes = Encoding.UTF8.GetBytes( JsonConvert.SerializeObject( JsonObject, Formatting.None ) );
			EventBriteRequest.ContentLength = RequestBytes.Length;
			using( Stream RequestStream = EventBriteRequest.GetRequestStream() )
				RequestStream.Write( RequestBytes, 0, RequestBytes.Length );

			try {
				using( HttpWebResponse EventBriteResponse = await EventBriteRequest.GetResponseAsync() as HttpWebResponse );
			} catch( WebException ) {
				return new HttpResponseMessage( HttpStatusCode.BadGateway );
			} catch( Exception ) {
				return new HttpResponseMessage( HttpStatusCode.InternalServerError );
			}

			// Store Code in DataStore
			NewStudentCode = NewStudentCode.WithId( ( await _DataStore.Add( NewStudentCode ) ).Id );

			// Email Code
			SendEmail( NewStudentCode );

			return new HttpResponseMessage( HttpStatusCode.Created );
		}

		private void SendEmail( FSharp.Common.StudentCode studentCode ) {
			MailMessage Mail = new MailMessage(
				new MailAddress( ConfigurationManager.AppSettings["SmtpEmailAddress"], ConfigurationManager.AppSettings["SmtpDisplayName"] ),
				new MailAddress( studentCode.Email )
			);

			Mail.Subject = "Student Ticket Code";
			Mail.Body = string.Format(
@"This email is a response to a request for a student discount code. We have validated your email address and are pleased to offer you this code.

This code is tied to your email address is a valid for one use. If you misplace this email, you may supply your email to the DevSpace website on the tickets page to receive another copy of this email. A new code will not be generated.

You may go directly to out ticketing page using the link below.
 
https://www.eventbrite.com/e/devspace-2016-registration-24347789895?access={0}

If you wish, you may also go directly to EventBrite, find out event, and manually enter the code:

{0}

We thank you for your interest in the DevSpace Technical Conference and look forward to seeing you there.", studentCode.Code );

			SmtpClient Client = new SmtpClient( ConfigurationManager.AppSettings["SmtpServer"], Convert.ToInt32( ConfigurationManager.AppSettings["SmtpPort"] ) );
			Client.EnableSsl = true;
			Client.Credentials = new NetworkCredential( ConfigurationManager.AppSettings["SmtpEmailAddress"], ConfigurationManager.AppSettings["SmtpPassword"] );
			Client.Send( Mail );
		}
	}
}
