// 계좌번호처럼 민감한 값을 브라우저에서 직접 암호화/복호화합니다.
// PIN은 절대 서버로 전송되지 않고, 암호화된 결과(ciphertext)만 저장됩니다.
// PBKDF2로 PIN에서 키를 만들고, AES-GCM으로 암호화합니다.

window.wondayCrypto = {
  async _deriveKey(pin, saltBase64) {
    const enc = new TextEncoder();
    const salt = saltBase64
      ? Uint8Array.from(atob(saltBase64), c => c.charCodeAt(0))
      : crypto.getRandomValues(new Uint8Array(16));

    const keyMaterial = await crypto.subtle.importKey(
      'raw', enc.encode(pin), { name: 'PBKDF2' }, false, ['deriveKey']
    );

    const key = await crypto.subtle.deriveKey(
      { name: 'PBKDF2', salt, iterations: 150000, hash: 'SHA-256' },
      keyMaterial,
      { name: 'AES-GCM', length: 256 },
      false,
      ['encrypt', 'decrypt']
    );

    return { key, salt };
  },

  _toBase64(bytes) {
    return btoa(String.fromCharCode(...new Uint8Array(bytes)));
  },

  _fromBase64(base64) {
    return Uint8Array.from(atob(base64), c => c.charCodeAt(0));
  },

  // 반환: { ciphertext, iv, salt } 전부 base64 문자열
  async encrypt(plaintext, pin) {
    const { key, salt } = await this._deriveKey(pin, null);
    const iv = crypto.getRandomValues(new Uint8Array(12));
    const enc = new TextEncoder();

    const ciphertext = await crypto.subtle.encrypt(
      { name: 'AES-GCM', iv }, key, enc.encode(plaintext)
    );

    return {
      ciphertext: this._toBase64(ciphertext),
      iv: this._toBase64(iv),
      salt: this._toBase64(salt)
    };
  },

  // PIN이 틀리면 예외가 발생합니다 (호출부에서 try/catch로 처리하세요).
  async decrypt(ciphertextBase64, ivBase64, saltBase64, pin) {
    const { key } = await this._deriveKey(pin, saltBase64);
    const iv = this._fromBase64(ivBase64);
    const ciphertext = this._fromBase64(ciphertextBase64);

    const plaintextBytes = await crypto.subtle.decrypt(
      { name: 'AES-GCM', iv }, key, ciphertext
    );

    return new TextDecoder().decode(plaintextBytes);
  },

  async copyToClipboard(text) {
    await navigator.clipboard.writeText(text);
  }
};
