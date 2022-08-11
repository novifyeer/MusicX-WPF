using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using VkNet.Model;

namespace VkNet.AudioBypassService.Exceptions
{
	[Serializable]
	public class VkAuthException : System.Exception
	{
		[NotNull]
		public BypassAuthError AuthError { get; }

		public VkAuthException([NotNull] BypassAuthError vkAuthError) : base(vkAuthError.ErrorDescription ?? vkAuthError.Error)
		{
			AuthError = vkAuthError;
		}
	}

	public class BypassAuthError : VkAuthError
	{
		[JsonProperty("validation_type")]
		public BypassValidationType ValidationType { get; set; }
		
		[JsonProperty("redirect_uri")]
		public string RedirectUri { get; set; }
		
		[JsonProperty("validation_sid")]
		public string ValidationSid { get; set; }
	}

	public enum BypassValidationType
	{
		None,
		[EnumMember(Value = "2fa_sms")]
		Sms,
		[EnumMember(Value = "2fa_callreset")]
		CallReset,
		[EnumMember(Value = "2fa_app")]
		App
	}
}