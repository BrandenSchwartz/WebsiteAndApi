﻿using System;
using System.Runtime.Serialization;

namespace DevSpace.Common {
	[DataContract]
	public class User : IUser {
		private User() {}

		[DataMember]public string Bio { get; private set; }
		[DataMember]	public string DisplayName { get; private set; }
		[DataMember]	public string EmailAddress { get; private set; }
		[DataMember]	public int Id { get; private set; }
		public string PasswordHash { get; private set; }
		public byte Permissions { get; private set; }
		[DataMember]public string Website { get; private set; }
		public Guid SessionToken { get; private set; }
		public DateTime SessionExpires { get; private set; }

		public IUser UpdateId( int newId ) {
			User newUser = Clone();
			newUser.Id = newId;
			return newUser;
		}

		public IUser UpdateDisplayName( string newDisplayName ) {
			User newUser = Clone();
			newUser.DisplayName = newDisplayName;
			return newUser;
		}

		public IUser UpdateEmailAddress( string newEmailAddress ) {
			User newUser = Clone();
			newUser.EmailAddress = newEmailAddress;
			return newUser;
		}

		public IUser UpdateBio( string newBio ) {
			User newUser = Clone();
			newUser.Bio = newBio;
			return newUser;
		}

		public IUser UpdatePermissions( byte newPermissions ) {
			User newUser = Clone();
			newUser.Permissions = newPermissions;
			return newUser;
		}

		public IUser UpdatePasswordHash( string newPasswordHash ) {
			User newUser = Clone();
			newUser.PasswordHash = newPasswordHash;
			return newUser;
		}

		public IUser UpdateWebsite( string newWebsite ) {
			User newUser = Clone();
			newUser.Website = newWebsite;
			return newUser;
		}

		public IUser UpdateSessionToken( Guid newSessionToken ) {
			User newUser = Clone();
			newUser.SessionToken = newSessionToken;
			return newUser;
		}

		public IUser UpdateSessionExpires( DateTime newSessionExpires ) {
			User newUser = Clone();
			newUser.SessionExpires = newSessionExpires;
			return newUser;
		}

		private User Clone() {
			User cloned = new User();
			cloned.Id = this.Id;
			cloned.DisplayName = string.Copy( this.DisplayName );
			cloned.EmailAddress = string.Copy( this.EmailAddress );
			cloned.Bio = string.Copy( this.Bio );
			cloned.Permissions = this.Permissions;
			cloned.PasswordHash = string.Copy( this.PasswordHash );
			cloned.Website = string.Copy( this.Website );
			cloned.SessionToken = this.SessionToken;
			cloned.SessionExpires = this.SessionExpires;
			return cloned;
		}
	}
}
