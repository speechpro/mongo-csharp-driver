﻿/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using MongoDB.Driver.Core.Authentication;
using MongoDB.Shared;

namespace MongoDB.Driver
{
    /// <summary>
    /// Credential to access a MongoDB database.
    /// </summary>
#if NET452
    [Serializable]
#endif
    public class MongoCredential : IEquatable<MongoCredential>
    {
        // private fields
        private readonly MongoIdentityEvidence _evidence;
        private readonly MongoIdentity _identity;
        private readonly string _mechanism;
        private readonly Dictionary<string, object> _mechanismProperties;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCredential" /> class.
        /// </summary>
        /// <param name="mechanism">Mechanism to authenticate with.
        /// In .NET Standard, authenticating via SCRAM-SHA-256 may not work with non-ASCII passwords because SaslPrep is
        /// not fully implemented due to the lack of a string normalization function in .NET Standard 1.5.
        /// Normalizing the password into Unicode Normalization Form KC beforehand MAY help.
        /// SCRAM-SHA-1 is the recommended alternative for now.</param>
        /// <param name="identity">The identity.</param>
        /// <param name="evidence">The evidence.</param>
        public MongoCredential(string mechanism, MongoIdentity identity, MongoIdentityEvidence evidence)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            if (evidence == null)
            {
                throw new ArgumentNullException("evidence");
            }

            _mechanism = mechanism;
            _identity = identity;
            _evidence = evidence;
            _mechanismProperties = new Dictionary<string, object>();
        }

        // public properties
        /// <summary>
        /// Gets the evidence.
        /// </summary>
        public MongoIdentityEvidence Evidence
        {
            get { return _evidence; }
        }

        /// <summary>
        /// Gets the identity.
        /// </summary>
        public MongoIdentity Identity
        {
            get { return _identity; }
        }

        /// <summary>
        /// Gets the mechanism to authenticate with.
        /// In .NET Standard, authenticating via SCRAM-SHA-256 may not work with non-ASCII passwords because SaslPrep is
        /// not fully implemented due to the lack of a string normalization function in .NET Standard 1.5.
        /// Normalizing the password into Unicode Normalization Form KC beforehand MAY help.
        /// SCRAM-SHA-1 is the recommended alternative for now.
        /// </summary>
        public string Mechanism
        {
            get { return _mechanism; }
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        [Obsolete("Use Evidence instead.")]
        public string Password
        {
            get
            {
                var passwordEvidence = _evidence as PasswordEvidence;
                if (passwordEvidence != null)
                {
                    return SecureStringHelper.ToInsecureString(passwordEvidence.SecurePassword);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the source.
        /// </summary>
        public string Source
        {
            get { return _identity.Source; }
        }

        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username
        {
            get { return _identity.Username; }
        }

        // public operators
        /// <summary>
        /// Compares two MongoCredentials.
        /// </summary>
        /// <param name="lhs">The first MongoCredential.</param>
        /// <param name="rhs">The other MongoCredential.</param>
        /// <returns>True if the two MongoCredentials are equal (or both null).</returns>
        public static bool operator ==(MongoCredential lhs, MongoCredential rhs)
        {
            return object.Equals(lhs, rhs);
        }

        /// <summary>
        /// Compares two MongoCredentials.
        /// </summary>
        /// <param name="lhs">The first MongoCredential.</param>
        /// <param name="rhs">The other MongoCredential.</param>
        /// <returns>True if the two MongoCredentials are not equal (or one is null and the other is not).</returns>
        public static bool operator !=(MongoCredential lhs, MongoCredential rhs)
        {
            return !(lhs == rhs);
        }

        // public static methods
        /// <summary>
        /// Creates a default credential.
        /// In .NET Standard, authenticating via SCRAM-SHA-256 may not work with non-ASCII passwords because SaslPrep is
        /// not fully implemented due to the lack of a string normalization function in .NET Standard 1.5.
        /// Normalizing the password into Unicode Normalization Form KC beforehand MAY help.
        /// SCRAM-SHA-1 is the recommended alternative for now.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A default credential.</returns>
        public static MongoCredential CreateCredential(string databaseName, string username, string password)
        {
            return FromComponents(
                mechanism: null,
                source: null,
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a default credential.
        /// Less secure when used in conjunction with SCRAM-SHA-256, due to the need to store the password in a managed
        /// string in order to SaslPrep it.
        /// In .NET Standard, authenticating via SCRAM-SHA-256 may not work with non-ASCII passwords because SaslPrep is
        /// not fully implemented due to the lack of a string normalization function in .NET Standard 1.5.
        /// Normalizing the password into Unicode Normalization Form KC beforehand MAY help.
        /// SCRAM-SHA-1 is the recommended alternative for now.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A default credential.</returns>
        public static MongoCredential CreateCredential(string databaseName, string username, SecureString password)
        {
            return FromComponents(
                mechanism: null,
                source: null,
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a GSSAPI credential.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>A credential for GSSAPI.</returns>
        /// <remarks>This overload is used primarily on linux.</remarks>
        public static MongoCredential CreateGssapiCredential(string username)
        {
            return FromComponents(
                mechanism: "GSSAPI",
                source: "$external",
                databaseName: null,
                username,
                new ExternalEvidence());
        }

        /// <summary>
        /// Creates a GSSAPI credential.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for GSSAPI.</returns>
        public static MongoCredential CreateGssapiCredential(string username, string password)
        {
            return FromComponents(
                mechanism: "GSSAPI",
                source: "$external",
                databaseName: null,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a GSSAPI credential.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for GSSAPI.</returns>
        public static MongoCredential CreateGssapiCredential(string username, SecureString password)
        {
            return FromComponents(
                mechanism: "GSSAPI",
                source: "$external",
                databaseName: null,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a credential used with MONGODB-CR.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for MONGODB-CR.</returns>
        [Obsolete("MONGODB-CR was replaced by SCRAM-SHA-1 in MongoDB 3.0, and is now deprecated.")]
        public static MongoCredential CreateMongoCRCredential(string databaseName, string username, string password)
        {
            return FromComponents(
                mechanism: "MONGODB-CR",
                source: null,
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a credential used with MONGODB-CR.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for MONGODB-CR.</returns>
        [Obsolete("MONGODB-CR was replaced by SCRAM-SHA-1 in MongoDB 3.0, and is now deprecated.")]
        public static MongoCredential CreateMongoCRCredential(string databaseName, string username, SecureString password)
        {
            return FromComponents(
                mechanism: "MONGODB-CR",
                source: null,
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a credential used with MONGODB-X509.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <returns>A credential for MONGODB-X509.</returns>
        public static MongoCredential CreateMongoX509Credential(string username = null)
        {
            return FromComponents(
                mechanism: "MONGODB-X509",
                source: "$external",
                databaseName: null,
                username,
                new ExternalEvidence());
        }

        /// <summary>
        /// Creates a PLAIN credential.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for PLAIN.</returns>
        public static MongoCredential CreatePlainCredential(string databaseName, string username, string password)
        {
            return FromComponents(
                mechanism: "PLAIN",
                source: null,
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        /// <summary>
        /// Creates a PLAIN credential.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns>A credential for PLAIN.</returns>
        public static MongoCredential CreatePlainCredential(string databaseName, string username, SecureString password)
        {
            return FromComponents(
                mechanism: "PLAIN",
                source: null,
                databaseName,
                username,
                new PasswordEvidence(password));
        }

        // public methods
        /// <summary>
        /// Gets the mechanism property.
        /// </summary>
        /// <typeparam name="T">The type of the mechanism property.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The mechanism property if one was set; otherwise the default value.</returns>
        public T GetMechanismProperty<T>(string key, T defaultValue)
        {
            object value;
            if (_mechanismProperties.TryGetValue(key, out value))
            {
                return (T)value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Compares this MongoCredential to another MongoCredential.
        /// </summary>
        /// <param name="rhs">The other credential.</param>
        /// <returns>True if the two credentials are equal.</returns>
        public bool Equals(MongoCredential rhs)
        {
            if (object.ReferenceEquals(rhs, null) || GetType() != rhs.GetType()) { return false; }
            return _identity == rhs._identity &&
                _evidence == rhs._evidence &&
                _mechanism == rhs._mechanism &&
                _mechanismProperties.OrderBy(x => x.Key).SequenceEqual(rhs._mechanismProperties.OrderBy(x => x.Key));
        }

        /// <summary>
        /// Compares this MongoCredential to another MongoCredential.
        /// </summary>
        /// <param name="obj">The other credential.</param>
        /// <returns>True if the two credentials are equal.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as MongoCredential); // works even if obj is null or of a different type
        }

        /// <summary>
        /// Gets the hashcode for the credential.
        /// </summary>
        /// <returns>The hashcode.</returns>
        public override int GetHashCode()
        {
            // see Effective Java by Joshua Bloch
            return new Hasher()
                .Hash(_identity)
                .Hash(_evidence)
                .Hash(_mechanism)
                .HashStructElements(_mechanismProperties)
                .GetHashCode();
        }

        /// <summary>
        /// Returns a string representation of the credential.
        /// </summary>
        /// <returns>A string representation of the credential.</returns>
        public override string ToString()
        {
            return string.Format("{0}@{1}", _identity.Username, _identity.Source);
        }

        /// <summary>
        /// Creates a new MongoCredential with the specified mechanism property.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>A new MongoCredential with the specified mechanism property.</returns>
        public MongoCredential WithMechanismProperty(string key, object value)
        {
            var copy = new MongoCredential(_mechanism, _identity, _evidence);
            foreach (var pair in _mechanismProperties)
            {
                copy._mechanismProperties.Add(pair.Key, pair.Value);
            }
            copy._mechanismProperties[key] = value; // overwrite if it's already set
            return copy;
        }

        // internal methods
        internal IAuthenticator ToAuthenticator(ServerApi serverApi)
        {
            var passwordEvidence = _evidence as PasswordEvidence;
            if (passwordEvidence != null)
            {
                var insecurePassword = SecureStringHelper.ToInsecureString(passwordEvidence.SecurePassword);
                var credential = new UsernamePasswordCredential(
                    _identity.Source,
                    _identity.Username,
                    insecurePassword);

                if (_mechanism == null)
                {
                    return new DefaultAuthenticator(credential, serverApi);
                }
#pragma warning disable 618
                if (_mechanism == MongoDBCRAuthenticator.MechanismName)
                {
                    return new MongoDBCRAuthenticator(credential, serverApi);
#pragma warning restore 618
                }
                if (_mechanism == ScramSha1Authenticator.MechanismName)
                {
                    return new ScramSha1Authenticator(credential, serverApi);
                }
                if (_mechanism == ScramSha256Authenticator.MechanismName)
                {
                    return new ScramSha256Authenticator(credential, serverApi);
                }
                if (_mechanism == PlainAuthenticator.MechanismName)
                {
                    return new PlainAuthenticator(credential, serverApi);
                }
                if (_mechanism == GssapiAuthenticator.MechanismName)
                {
                    return new GssapiAuthenticator(
                        credential,
                        _mechanismProperties.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())),
                        serverApi);
                }
                if (_mechanism == MongoAWSAuthenticator.MechanismName)
                {
                    return new MongoAWSAuthenticator(
                        credential,
                        _mechanismProperties.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())),
                        serverApi);
                }
            }
            else if (_identity.Source == "$external" && _evidence is ExternalEvidence)
            {
                if (_mechanism == MongoDBX509Authenticator.MechanismName)
                {
                    return new MongoDBX509Authenticator(_identity.Username, serverApi);
                }
                if (_mechanism == GssapiAuthenticator.MechanismName)
                {
                    return new GssapiAuthenticator(
                        _identity.Username,
                        _mechanismProperties.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())),
                        serverApi);
                }
                if (_mechanism == MongoAWSAuthenticator.MechanismName)
                {
                    return new MongoAWSAuthenticator(
                        _identity.Username,
                        _mechanismProperties.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())),
                        serverApi);
                }
            }

            throw new NotSupportedException("Unable to create an authenticator.");
        }

        // internal static methods
        internal static MongoCredential FromComponents(string mechanism, string source, string username, string password)
        {
            return FromComponents(mechanism, source, databaseName: null, username, password);
        }

        internal static MongoCredential FromComponents(string mechanism, string source, string databaseName, string username, string password)
        {
            var evidence = password == null ? (MongoIdentityEvidence)new ExternalEvidence() : new PasswordEvidence(password);
            return FromComponents(mechanism, source, databaseName, username, evidence);
        }

        // private methods
        private void ValidatePassword(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            if (password.Any(c => (int)c >= 128))
            {
                throw new ArgumentException("Password must contain only ASCII characters.");
            }
        }

        // private static methods
        private static void EnsureNullOrExternalSource(string mechanism, string source)
        {
            if (source != null && source != "$external")
            {
                throw new ArgumentException($"A {mechanism} source must be $external.", nameof(source));
            }
        }

        private static MongoCredential FromComponents(string mechanism, string source, string databaseName, string username, MongoIdentityEvidence evidence)
        {
            var defaultedMechanism = (mechanism ?? "DEFAULT").Trim().ToUpperInvariant();
            switch (defaultedMechanism)
            {
                case "DEFAULT":
                case "MONGODB-CR":
                case "SCRAM-SHA-1":
                case "SCRAM-SHA-256":
                    // it is allowed for a password to be an empty string, but not a username
                    source = source ?? databaseName ?? "admin";
                    if (evidence == null || !(evidence is PasswordEvidence))
                    {
                        var message = string.Format("A {0} credential must have a password.", defaultedMechanism);
                        throw new ArgumentException(message);
                    }

                    return new MongoCredential(
                        mechanism,
                        new MongoInternalIdentity(source, username),
                        evidence);
                case "MONGODB-AWS":
                    // MUST be "$external". Defaults to $external.
                    EnsureNullOrExternalSource(mechanism, source);
                    if (username == null)
                    {
                        if (evidence is PasswordEvidence)
                        {
                            throw new ArgumentException("A MONGODB-AWS credential must have an access key id.");
                        }

                        return new MongoCredential(
                            mechanism,
                            new MongoExternalAwsIdentity(),
                            evidence);
                    }
                    if (evidence == null || evidence is ExternalEvidence)
                    {
                        throw new ArgumentException("A MONGODB-AWS credential must have a secret access key.");
                    }

                    return new MongoCredential(
                        mechanism,
                        new MongoExternalIdentity(username),
                        evidence);
                case "MONGODB-X509":
                    // MUST be "$external". Defaults to $external.
                    EnsureNullOrExternalSource(mechanism, source);
                    if (evidence == null || !(evidence is ExternalEvidence))
                    {
                        throw new ArgumentException("A MONGODB-X509 does not support a password.");
                    }

                    return new MongoCredential(
                        mechanism,
                        new MongoX509Identity(username),
                        evidence);
                case "GSSAPI":
                    // MUST be "$external". Defaults to $external.
                    EnsureNullOrExternalSource(mechanism, source);

                    return new MongoCredential(
                        mechanism,
                        new MongoExternalIdentity(username),
                        evidence);
                case "PLAIN":
                    source = source ?? databaseName ?? "$external";
                    if (evidence == null || !(evidence is PasswordEvidence))
                    {
                        throw new ArgumentException("A PLAIN credential must have a password.");
                    }

                    MongoIdentity identity;
                    if (source == "$external")
                    {
                        identity = new MongoExternalIdentity(username);
                    }
                    else
                    {
                        identity = new MongoInternalIdentity(source, username);
                    }

                    return new MongoCredential(
                        mechanism,
                        identity,
                        evidence);
                default:
                    throw new NotSupportedException(string.Format("Unsupported MongoAuthenticationMechanism {0}.", mechanism));
            }
        }
    }
}
