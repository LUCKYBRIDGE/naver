/** Student-only two-set Hangul composition; no Windows IME calls. */
(() => {
  const initial = [...'ㄱㄲㄴㄷㄸㄹㅁㅂㅃㅅㅆㅇㅈㅉㅊㅋㅌㅍㅎ'];
  const vowel = [...'ㅏㅐㅑㅒㅓㅔㅕㅖㅗㅘㅙㅚㅛㅜㅝㅞㅟㅠㅡㅢㅣ'];
  const final = ['', ...'ㄱㄲㄳㄴㄵㄶㄷㄹㄺㄻㄼㄽㄾㄿㅀㅁㅂㅄㅅㅆㅇㅈㅊㅋㅌㅍㅎ'];
  const vowels = { 'ㅗㅏ': 'ㅘ', 'ㅗㅐ': 'ㅙ', 'ㅗㅣ': 'ㅚ', 'ㅜㅓ': 'ㅝ', 'ㅜㅔ': 'ㅞ', 'ㅜㅣ': 'ㅟ', 'ㅡㅣ': 'ㅢ' };
  const finals = { 'ㄱㅅ': 'ㄳ', 'ㄴㅈ': 'ㄵ', 'ㄴㅎ': 'ㄶ', 'ㄹㄱ': 'ㄺ', 'ㄹㅁ': 'ㄻ', 'ㄹㅂ': 'ㄼ', 'ㄹㅅ': 'ㄽ', 'ㄹㅌ': 'ㄾ', 'ㄹㅍ': 'ㄿ', 'ㄹㅎ': 'ㅀ', 'ㅂㅅ': 'ㅄ' };
  const empty = () => ({ l: -1, v: -1, t: 0 });
  class Composer {
    constructor() { this.reset(); }
    reset() { this.state = empty(); this.undo = []; }
    get pending() {
      const { l, v, t } = this.state;
      if (l < 0) return v < 0 ? '' : vowel[v];
      return v < 0 ? initial[l] : String.fromCharCode(0xac00 + (l * 21 + v) * 28 + t);
    }
    flush() { const value = this.pending; this.reset(); return value; }
    backspace() { if (!this.undo.length) return false; this.state = this.undo.pop(); return true; }
    push(char) {
      const before = { ...this.state }, s = this.state;
      let commit = '', seed = null;
      const vi = vowel.indexOf(char), li = initial.indexOf(char), ti = final.indexOf(char);
      if (vi >= 0) {
        if (s.v < 0) s.v = vi;
        else if (s.t) {
          const split = Object.entries(finals).find(([, value]) => value === final[s.t]);
          const moved = split ? split[0][1] : final[s.t];
          s.t = split ? final.indexOf(split[0][0]) : 0;
          commit = this.pending;
          this.state = { l: initial.indexOf(moved), v: vi, t: 0 };
          seed = { l: initial.indexOf(moved), v: -1, t: 0 };
        } else {
          const combined = vowels[vowel[s.v] + char];
          if (combined) s.v = vowel.indexOf(combined);
          else { commit = this.pending; this.state = { l: -1, v: vi, t: 0 }; }
        }
      } else if (li >= 0) {
        if (s.l < 0 && s.v < 0) s.l = li;
        else if (s.l >= 0 && s.v >= 0 && s.t === 0 && ti > 0) s.t = ti;
        else if (s.t > 0 && finals[final[s.t] + char]) s.t = final.indexOf(finals[final[s.t] + char]);
        else { commit = this.pending; this.state = { l: li, v: -1, t: 0 }; }
      } else { commit = this.flush() + char; }
      if (commit) this.undo = this.pending ? (seed ? [empty(), seed] : [empty()]) : [];
      else this.undo.push(before);
      return { commit, pending: this.pending };
    }
  }
  globalThis.WDIHangul = Composer;
})();
