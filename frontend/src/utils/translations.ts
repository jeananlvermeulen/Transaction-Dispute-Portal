export interface Translations {
  // Language selector
  preferredLanguage: string
  // Login
  email: string
  password: string
  signIn: string
  signingIn: string
  noAccount: string
  signUp: string
  needAccount: string
  registerHere: string
  customerLogin: string
  portalSubtitle: string
  employeeSubtitle: string
  // Register
  createAccount: string
  creatingAccount: string
  joinPortal: string
  firstName: string
  lastName: string
  phoneNumber: string
  confirmPassword: string
  alreadyHaveAccount: string
  accountCreated: string
  accountCreatedMsg: string
  clickToLogin: string
  // Nav
  transactions: string
  disputes: string
  security: string
  logout: string
  dashboard: string
  // Dashboard
  welcome: string
  accountNumber: string
  quickActions: string
  viewTransactions: string
  viewDisputes: string
  mfaEnabled: string
  mfaDisabled: string
  loading: string
  failedToLoad: string
  // Transactions
  simulateTransactions: string
  simulateDesc: string
  simulate: string
  simulating: string
  noTransactions: string
  amount: string
  description: string
  date: string
  status: string
  action: string
  disputeTransaction: string
  // Disputes
  myDisputes: string
  noDisputes: string
  reason: string
  summary: string
  reference: string
  detailsOfDispute: string
  // Dispute modal
  disputeStep1Title: string
  selectReason: string
  next: string
  back: string
  cancel: string
  disputeStep2Title: string
  writeSummary: string
  summaryPlaceholder: string
  submitDispute: string
  submitting: string
  disputeSubmitted: string
  disputeSubmittedMsg: string
  close: string
  // Dispute reasons
  reasonUnauthorised: string
  reasonIncorrectAmount: string
  reasonDoublePayment: string
  reasonOther: string
  otherReasonPlaceholder: string
  // Security
  securitySettings: string
  // Profile
  profile: string
  editProfile: string
  updateProfile: string
  updating: string
  profileUpdated: string
  changePassword: string
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
  passwordChanged: string
  saveChanges: string
  saving: string
}

export const LANGUAGES: { code: string; label: string }[] = [
  { code: 'en', label: 'English' },
  { code: 'zu', label: 'isiZulu' },
  { code: 'xh', label: 'isiXhosa' },
  { code: 'af', label: 'Afrikaans' },
  { code: 'nso', label: 'Sepedi' },
  { code: 'tn', label: 'Setswana' },
  { code: 'st', label: 'Sesotho' },
  { code: 'ts', label: 'Xitsonga' },
  { code: 'ss', label: 'siSwati' },
  { code: 've', label: 'Tshivenda' },
  { code: 'nr', label: 'isiNdebele' },
]

const t: Record<string, Translations> = {
  en: {
    preferredLanguage: 'Preferred Language',
    email: 'Email', password: 'Password', signIn: 'Sign In', signingIn: 'Signing in...',
    noAccount: "Don't have an account?", signUp: 'Sign up', needAccount: 'Need an account?',
    registerHere: 'Register here', customerLogin: 'Customer login',
    portalSubtitle: 'Transaction Dispute Portal', employeeSubtitle: 'Employee Portal',
    createAccount: 'Create Account', creatingAccount: 'Creating Account...', joinPortal: "Join Capitec's Dispute Portal",
    firstName: 'First Name', lastName: 'Last Name', phoneNumber: 'Phone Number',
    confirmPassword: 'Confirm Password', alreadyHaveAccount: 'Already have an account?',
    accountCreated: 'Account Created!', accountCreatedMsg: 'Your account has been created successfully.',
    clickToLogin: 'Click anywhere to go to the login screen',
    transactions: 'Transactions', disputes: 'Disputes', security: 'Security', logout: 'Logout', dashboard: 'Dashboard',
    welcome: 'Welcome', accountNumber: 'Account Number', quickActions: 'Quick Actions',
    viewTransactions: 'View Transactions', viewDisputes: 'View Disputes',
    mfaEnabled: '✓ MFA Enabled', mfaDisabled: '! MFA Disabled', loading: 'Loading...', failedToLoad: 'Failed to load user data',
    simulateTransactions: 'Simulate Transactions', simulateDesc: 'Generates 20 sample transactions for testing',
    simulate: 'Simulate', simulating: 'Simulating...', noTransactions: 'No transactions found',
    amount: 'Amount', description: 'Description', date: 'Date', status: 'Status', action: 'Action',
    disputeTransaction: 'Dispute Transaction',
    myDisputes: 'My Disputes', noDisputes: 'No disputes found', reason: 'Reason', summary: 'Summary',
    reference: 'Reference', detailsOfDispute: 'Details of Dispute',
    disputeStep1Title: 'Why are you disputing?', selectReason: 'Select a reason',
    next: 'Next', back: 'Back', cancel: 'Cancel',
    disputeStep2Title: 'Provide more details', writeSummary: 'Write a brief summary',
    summaryPlaceholder: 'Describe the issue in more detail...', submitDispute: 'Submit Dispute', submitting: 'Submitting...',
    disputeSubmitted: 'Dispute Submitted!', disputeSubmittedMsg: 'Your dispute has been submitted successfully.',
    close: 'Close',
    reasonUnauthorised: 'Unauthorised Transaction', reasonIncorrectAmount: 'Incorrect Amount',
    reasonDoublePayment: 'Double Payment', reasonOther: 'Other',
    otherReasonPlaceholder: 'Please describe your reason...',
    securitySettings: 'Security Settings',
    profile: 'Profile', editProfile: 'Edit Profile', updateProfile: 'Update Profile',
    updating: 'Updating...', profileUpdated: 'Profile updated successfully!',
    changePassword: 'Change Password', currentPassword: 'Current Password',
    newPassword: 'New Password', confirmNewPassword: 'Confirm New Password',
    passwordChanged: 'Password changed successfully!',
    saveChanges: 'Save Changes', saving: 'Saving...',
  },
  zu: {
    preferredLanguage: 'Ulimi Oluthandekayo',
    email: 'I-imeyili', password: 'Iphasiwedi', signIn: 'Ngena', signingIn: 'Uyangena...',
    noAccount: 'Awunayo i-akhawunti?', signUp: 'Bhalisa', needAccount: 'Udinga i-akhawunti?',
    registerHere: 'Bhalisa lapha', customerLogin: 'Ukungena kwamakhasimende',
    portalSubtitle: 'Ikhuliso Lezinkinga Zezinkokhelo', employeeSubtitle: 'Ikhuliso Labasebenzi',
    createAccount: 'Dala I-akhawunti', creatingAccount: 'Idala I-akhawunti...', joinPortal: 'Joyina Ikhuliso Lezinkinga',
    firstName: 'Igama', lastName: 'Isibongo', phoneNumber: 'Inombolo Yocingo',
    confirmPassword: 'Qinisekisa Iphasiwedi', alreadyHaveAccount: 'Usunayo i-akhawunti?',
    accountCreated: 'I-akhawunti Idaliwe!', accountCreatedMsg: 'I-akhawunti yakho idaliwe ngempumelelo.',
    clickToLogin: 'Chofoza noma kuphi ukuya esikrini sokungena',
    transactions: 'Izinkokhelo', disputes: 'Izinkinga', security: 'Ukuphepha', logout: 'Phuma', dashboard: 'Ibhodi',
    welcome: 'Wamukelwa', accountNumber: 'Inombolo ye-akhawunti', quickActions: 'Izenzo Ezisheshayo',
    viewTransactions: 'Buka Izinkokhelo', viewDisputes: 'Buka Izinkinga',
    mfaEnabled: '✓ I-MFA Inikwe Amandla', mfaDisabled: '! I-MFA Ikhuthaziwe', loading: 'Iyalayisha...', failedToLoad: 'Yehlulekile ukulayisha idatha',
    simulateTransactions: 'Lingisa Izinkokhelo', simulateDesc: 'Ikhiqiza izinkokhelo eziyi-20 zokuhlola',
    simulate: 'Lingisa', simulating: 'Iyalingisa...', noTransactions: 'Azikho izinkokhelo',
    amount: 'Inani', description: 'Incazelo', date: 'Usuku', status: 'Isimo', action: 'Isenzo',
    disputeTransaction: 'Phikisa Inkokhelo',
    myDisputes: 'Izinkinga Zami', noDisputes: 'Azikho izinkinga', reason: 'Isizathu', summary: 'Isifinyezo',
    reference: 'Inombolo Yokukhomba', detailsOfDispute: 'Imininingwane Yezinkinga',
    disputeStep1Title: 'Kungani uphikisa?', selectReason: 'Khetha isizathu',
    next: 'Okulandelayo', back: 'Emuva', cancel: 'Khansela',
    disputeStep2Title: 'Nikeza imininingwane engaphezulu', writeSummary: 'Bhala isifinyezo',
    summaryPlaceholder: 'Chaza inkinga ngokuningiliziwe...', submitDispute: 'Thumela Isikhalazo', submitting: 'Iyathumela...',
    disputeSubmitted: 'Isikhalazo Sithunyiwe!', disputeSubmittedMsg: 'Isikhalazo sakho sithunyiwe ngempumelelo.',
    close: 'Vala',
    reasonUnauthorised: 'Inkokhelo Engagunyaziwe', reasonIncorrectAmount: 'Inani Elingalungile',
    reasonDoublePayment: 'Inkokhelo Ephindwe Kabili', reasonOther: 'Okunye',
    otherReasonPlaceholder: 'Chaza isizathu sakho...',
    securitySettings: 'Izilungiselelo Zokuphepha',
    profile: 'Iphrofayili', editProfile: 'Hlela Iphrofayili', updateProfile: 'Buyekeza Iphrofayili',
    updating: 'Iyabuyekeza...', profileUpdated: 'Iphrofayili ibuyekezwe!',
    changePassword: 'Shintsha Iphasiwedi', currentPassword: 'Iphasiwedi Yamanje',
    newPassword: 'Iphasiwedi Entsha', confirmNewPassword: 'Qinisekisa Iphasiwedi Entsha',
    passwordChanged: 'Iphasiwedi ishintshiwe!',
    saveChanges: 'Gcina Izinguquko', saving: 'Igcina...',
  },
  xh: {
    preferredLanguage: 'Ulwimi Oluthandekayo',
    email: 'I-imeyile', password: 'Ipasiwedi', signIn: 'Ngena', signingIn: 'Uyangena...',
    noAccount: 'Awunayo i-akhawunti?', signUp: 'Bhalisa', needAccount: 'Ufuna i-akhawunti?',
    registerHere: 'Bhalisa apha', customerLogin: 'Ukungena kwabathengi',
    portalSubtitle: 'Iikhalazo Zeentlawulo', employeeSubtitle: 'Isango Labasebenzi',
    createAccount: 'Yenza I-akhawunti', creatingAccount: 'Iyenza I-akhawunti...', joinPortal: 'Joyina Isango Sezikhalazo',
    firstName: 'Igama', lastName: 'Ifani', phoneNumber: 'Inombolo Yefoni',
    confirmPassword: 'Qinisekisa Ipasiwedi', alreadyHaveAccount: 'Usunayo i-akhawunti?',
    accountCreated: 'I-akhawunti Yenziwe!', accountCreatedMsg: 'I-akhawunti yakho yenziwe ngempumelelo.',
    clickToLogin: 'Cofa naphi ukuya kwisikrini sokungena',
    transactions: 'Iintlawulo', disputes: 'Iikhalazo', security: 'Ukhuseleko', logout: 'Phuma', dashboard: 'Ibhodi',
    welcome: 'Wamkelekile', accountNumber: 'Inombolo ye-akhawunti', quickActions: 'Izenzo Ezikhawulezayo',
    viewTransactions: 'Jonga Iintlawulo', viewDisputes: 'Jonga Iikhalazo',
    mfaEnabled: '✓ I-MFA Ivuliwe', mfaDisabled: '! I-MFA Ivaliwe', loading: 'Iyalayisha...', failedToLoad: 'Yehlulekile ukulayisha idatha',
    simulateTransactions: 'Lingisa Iintlawulo', simulateDesc: 'Ivelisa iintlawulo eziyi-20 zovavanyo',
    simulate: 'Lingisa', simulating: 'Iyalingisa...', noTransactions: 'Azikho iintlawulo',
    amount: 'Isixa', description: 'Inkcazelo', date: 'Umhla', status: 'Imeko', action: 'Isenzo',
    disputeTransaction: 'Phikisa Intlawulo',
    myDisputes: 'Iikhalazo Zam', noDisputes: 'Azikho iikhalazo', reason: 'Isizathu', summary: 'Isishwankathelo',
    reference: 'Inombolo Yokubhekisa', detailsOfDispute: 'Iinkcukacha Zekhalazo',
    disputeStep1Title: 'Kutheni uphikisa?', selectReason: 'Khetha isizathu',
    next: 'Okulandelayo', back: 'Emuva', cancel: 'Rhoxisa',
    disputeStep2Title: 'Nika iinkcukacha ezongezelelekileyo', writeSummary: 'Bhala isishwankathelo',
    summaryPlaceholder: 'Chaza ingxaki ngokunzulu...', submitDispute: 'Thumela Isikhalazo', submitting: 'Ithumela...',
    disputeSubmitted: 'Isikhalazo Sithunyiwe!', disputeSubmittedMsg: 'Isikhalazo sakho sithunyiwe ngempumelelo.',
    close: 'Vala',
    reasonUnauthorised: 'Intlawulo Engagunyaziswanga', reasonIncorrectAmount: 'Isixa Esingachanekanga',
    reasonDoublePayment: 'Intlawulo Ephindwe Kabini', reasonOther: 'Enye',
    otherReasonPlaceholder: 'Chaza isizathu sakho...',
    securitySettings: 'Iilungiselelo Zokhuseleko',
    profile: 'Iprofayile', editProfile: 'Hlela Iprofayile', updateProfile: 'Buyekeza Iprofayile',
    updating: 'Iyabuyekeza...', profileUpdated: 'Iprofayile ibuyekezwe!',
    changePassword: 'Tshintsha Ipasiwedi', currentPassword: 'Ipasiwedi Yangoku',
    newPassword: 'Ipasiwedi Entsha', confirmNewPassword: 'Qinisekisa Ipasiwedi Entsha',
    passwordChanged: 'Ipasiwedi itshintshiwe!',
    saveChanges: 'Gcina Utshintsho', saving: 'Igcina...',
  },
  af: {
    preferredLanguage: 'Voorkeurstaal',
    email: 'E-pos', password: 'Wagwoord', signIn: 'Teken In', signingIn: 'Besig om in te teken...',
    noAccount: "Het jy nie 'n rekening nie?", signUp: 'Registreer', needAccount: "Benodig jy 'n rekening?",
    registerHere: 'Registreer hier', customerLogin: 'Kliënt aanmelding',
    portalSubtitle: 'Transaksie Geskil Portaal', employeeSubtitle: 'Werknemers Portaal',
    createAccount: 'Skep Rekening', creatingAccount: 'Skep Rekening...', joinPortal: "Sluit aan by Capitec se Geskil Portaal",
    firstName: 'Voornaam', lastName: 'Van', phoneNumber: 'Telefoonnommer',
    confirmPassword: 'Bevestig Wagwoord', alreadyHaveAccount: 'Het jy reeds n rekening?',
    accountCreated: 'Rekening Geskep!', accountCreatedMsg: 'Jou rekening is suksesvol geskep.',
    clickToLogin: 'Klik enige plek om na die aanmeldskerm te gaan',
    transactions: 'Transaksies', disputes: 'Geskille', security: 'Sekuriteit', logout: 'Teken Uit', dashboard: 'Paneelbord',
    welcome: 'Welkom', accountNumber: 'Rekeningnommer', quickActions: 'Vinnige Aksies',
    viewTransactions: 'Bekyk Transaksies', viewDisputes: 'Bekyk Geskille',
    mfaEnabled: '✓ MFA Geaktiveer', mfaDisabled: '! MFA Gedeaktiveer', loading: 'Laai tans...', failedToLoad: 'Kon nie gebruikersdata laai nie',
    simulateTransactions: 'Simuleer Transaksies', simulateDesc: 'Genereer 20 voorbeeldtransaksies vir toetsing',
    simulate: 'Simuleer', simulating: 'Besig om te simuleer...', noTransactions: 'Geen transaksies gevind nie',
    amount: 'Bedrag', description: 'Beskrywing', date: 'Datum', status: 'Status', action: 'Aksie',
    disputeTransaction: 'Betwis Transaksie',
    myDisputes: 'My Geskille', noDisputes: 'Geen geskille gevind nie', reason: 'Rede', summary: 'Opsomming',
    reference: 'Verwysingnommer', detailsOfDispute: 'Besonderhede van Geskil',
    disputeStep1Title: 'Waarom betwis jy?', selectReason: 'Kies n rede',
    next: 'Volgende', back: 'Terug', cancel: 'Kanselleer',
    disputeStep2Title: 'Verskaf meer besonderhede', writeSummary: 'Skryf n kort opsomming',
    summaryPlaceholder: 'Beskryf die probleem in meer detail...', submitDispute: 'Dien Geskil In', submitting: 'Besig om in te dien...',
    disputeSubmitted: 'Geskil Ingedien!', disputeSubmittedMsg: 'Jou geskil is suksesvol ingedien.',
    close: 'Sluit',
    reasonUnauthorised: 'Ongemagtigde Transaksie', reasonIncorrectAmount: 'Verkeerde Bedrag',
    reasonDoublePayment: 'Dubbele Betaling', reasonOther: 'Ander',
    otherReasonPlaceholder: 'Beskryf jou rede...',
    securitySettings: 'Sekuriteitsinstellings',
    profile: 'Profiel', editProfile: 'Wysig Profiel', updateProfile: 'Dateer Profiel Op',
    updating: 'Besig om op te dateer...', profileUpdated: 'Profiel suksesvol opgedateer!',
    changePassword: 'Verander Wagwoord', currentPassword: 'Huidige Wagwoord',
    newPassword: 'Nuwe Wagwoord', confirmNewPassword: 'Bevestig Nuwe Wagwoord',
    passwordChanged: 'Wagwoord suksesvol verander!',
    saveChanges: 'Stoor Veranderinge', saving: 'Stoor...',
  },
  nso: {
    preferredLanguage: 'Polelo ye o e Ratago',
    email: 'Imeile', password: 'Phasewete', signIn: 'Tsena', signingIn: 'O a tsena...',
    noAccount: 'Ga o na akhaonte?', signUp: 'Ngwadiša', needAccount: 'O hloka akhaonte?',
    registerHere: 'Ngwadiša mo', customerLogin: 'Tsena ya moreki',
    portalSubtitle: 'Porotale ya Dikhikhino tša Ditšheletšo', employeeSubtitle: 'Porotale ya Basomedi',
    createAccount: 'Bopa Akhaonte', creatingAccount: 'E bopa Akhaonte...', joinPortal: 'Tsena Porotale ya Dikhikhino',
    firstName: 'Leina', lastName: 'Sefane', phoneNumber: 'Nomoramo ya Mogala',
    confirmPassword: 'Netefatša Phasewete', alreadyHaveAccount: 'O setše o na le akhaonte?',
    accountCreated: 'Akhaonte E Bopilwe!', accountCreatedMsg: 'Akhaonte ya gago e bopilwe ka katlego.',
    clickToLogin: 'Tobetša mo go ya go skrine ya go tsena',
    transactions: 'Ditirišano', disputes: 'Dikhikhino', security: 'Poloko', logout: 'Tswa', dashboard: 'Lepokisi',
    welcome: 'O amogelwa', accountNumber: 'Nomoramo ya Akhaonte', quickActions: 'Diketšo tša Bjako',
    viewTransactions: 'Lebelela Ditirišano', viewDisputes: 'Lebelela Dikhikhino',
    mfaEnabled: '✓ MFA E Kgontšhitšwe', mfaDisabled: '! MFA E Khutšitšwe', loading: 'E a laiša...', failedToLoad: 'Go paletšwe go laela data',
    simulateTransactions: 'Ingwadišo ya Ditirišano', simulateDesc: 'E hlola ditirišano tše 20 tša go leka',
    simulate: 'Ingwadišo', simulating: 'E a ingwadišo...', noTransactions: 'Ga go na ditirišano',
    amount: 'Tšhelete', description: 'Tlhalošo', date: 'Letšatši', status: 'Maemo', action: 'Ketšo',
    disputeTransaction: 'Phikišana le Tirišano',
    myDisputes: 'Dikhikhino Tša Gago', noDisputes: 'Ga go na dikhikhino', reason: 'Lebaka', summary: 'Kakaretšo',
    reference: 'Nomoramo ya Kgomišo', detailsOfDispute: 'Dintlha tša Khikhino',
    disputeStep1Title: 'Ke ka lebaka la eng o a phikišana?', selectReason: 'Kgetha lebaka',
    next: 'Ye e latelago', back: 'Morago', cancel: 'Khansela',
    disputeStep2Title: 'Fa dintlha tše dingwe', writeSummary: 'Ngwala kakaretšo',
    summaryPlaceholder: 'Hlaloša bothata ka botlalo...', submitDispute: 'Romela Khikhino', submitting: 'E a romela...',
    disputeSubmitted: 'Khikhino E Rometšwe!', disputeSubmittedMsg: 'Khikhino ya gago e rometšwe ka katlego.',
    close: 'Kwala',
    reasonUnauthorised: 'Tirišano ye e sa Dumeletšwego', reasonIncorrectAmount: 'Tšhelete ye e sa Lokago',
    reasonDoublePayment: 'Tefo e Dirišitšwe Gabedi', reasonOther: 'Tše Dingwe',
    otherReasonPlaceholder: 'Hlaloša lebaka la gago...',
    securitySettings: 'Ditlwaelo tša Poloko',
    profile: 'Profhaele', editProfile: 'Fetola Profhaele', updateProfile: 'Mpshafatša Profhaele',
    updating: 'E mpshafatša...', profileUpdated: 'Profhaele e mpshafatšitšwe!',
    changePassword: 'Fetola Phasewete', currentPassword: 'Phasewete ya Bjale',
    newPassword: 'Phasewete ye Mpsha', confirmNewPassword: 'Netefatša Phasewete ye Mpsha',
    passwordChanged: 'Phasewete e fetotšwe!',
    saveChanges: 'Boloka Diphetogo', saving: 'E boloka...',
  },
  tn: {
    preferredLanguage: 'Puo e o e Ratang',
    email: 'Imeile', password: 'Lefoko la Sephiri', signIn: 'Tsena', signingIn: 'O tsena...',
    noAccount: 'Ga o na akhaonto?', signUp: 'Ikwadise', needAccount: 'O tlhoka akhaonto?',
    registerHere: 'Ikwadise fa', customerLogin: 'Tsena ya moreki',
    portalSubtitle: 'Porotale ya Ditshiamelo tsa Ditirelo', employeeSubtitle: 'Porotale ya Bašomi',
    createAccount: 'Bopa Akhaonto', creatingAccount: 'E bopa Akhaonto...', joinPortal: 'Tsena Porotale ya Ditshiamelo',
    firstName: 'Leina', lastName: 'Sefane', phoneNumber: 'Nomoro ya Mogala',
    confirmPassword: 'Netefatsa Lefoko la Sephiri', alreadyHaveAccount: 'O setse o na le akhaonto?',
    accountCreated: 'Akhaonto e Bopilwe!', accountCreatedMsg: 'Akhaonto ya gago e bopilwe ka katlogo.',
    clickToLogin: 'Tlhatlhela kwa go ya kwa skerining ya go tsena',
    transactions: 'Ditirišano', disputes: 'Ditshiamelo', security: 'Tšhireletso', logout: 'Tswa', dashboard: 'Lepokisi',
    welcome: 'O amogelwa', accountNumber: 'Nomoro ya Akhaonto', quickActions: 'Diketso tsa Bonako',
    viewTransactions: 'Bona Ditirišano', viewDisputes: 'Bona Ditshiamelo',
    mfaEnabled: '✓ MFA e Kgontshiwa', mfaDisabled: '! MFA e Khutlela', loading: 'E a laisa...', failedToLoad: 'Go paletse go laisa data',
    simulateTransactions: 'Akanya Ditirišano', simulateDesc: 'E dira ditirišano di le 20 go leka',
    simulate: 'Akanya', simulating: 'E a akanya...', noTransactions: 'Ga go na ditirišano',
    amount: 'Tšhelete', description: 'Tlhaloso', date: 'Letsatsi', status: 'Maemo', action: 'Ketso',
    disputeTransaction: 'Ganela Tirišano',
    myDisputes: 'Ditshiamelo Tsa Me', noDisputes: 'Ga go na ditshiamelo', reason: 'Lebaka', summary: 'Kakaretso',
    reference: 'Nomoro ya Kgolagano', detailsOfDispute: 'Dintlha tsa Tshiamelo',
    disputeStep1Title: 'Ke ka lebaka la eng o ganela?', selectReason: 'Tlhopha lebaka',
    next: 'E e latelang', back: 'Morago', cancel: 'Khansela',
    disputeStep2Title: 'Fa dintlha tse dingwe', writeSummary: 'Kwala kakaretso',
    summaryPlaceholder: 'Tlhalosa bothata ka botlalo...', submitDispute: 'Romela Tshiamelo', submitting: 'E a romela...',
    disputeSubmitted: 'Tshiamelo e Rometšwe!', disputeSubmittedMsg: 'Tshiamelo ya gago e rometšwe ka katlogo.',
    close: 'Kwala',
    reasonUnauthorised: 'Tirišano e e sa Letlelelwang', reasonIncorrectAmount: 'Tšhelete e e Fosang',
    reasonDoublePayment: 'Tefo e Dirwa Gabedi', reasonOther: 'Tse Dingwe',
    otherReasonPlaceholder: 'Tlhalosa lebaka la gago...',
    securitySettings: 'Ditlhomeso tsa Tšhireletso',
    profile: 'Profaele', editProfile: 'Fetola Profaele', updateProfile: 'Mpshafatsa Profaele',
    updating: 'E mpshafatsa...', profileUpdated: 'Profaele e mpshafatsitswe!',
    changePassword: 'Fetola Lefoko la Sephiri', currentPassword: 'Lefoko la Sephiri la Jaanong',
    newPassword: 'Lefoko la Sephiri le Jotwa', confirmNewPassword: 'Netefatsa Lefoko la Sephiri le Jotwa',
    passwordChanged: 'Lefoko la Sephiri le fetotswe!',
    saveChanges: 'Boloka Diphetogo', saving: 'E boloka...',
  },
  st: {
    preferredLanguage: 'Puo e o e Ratang',
    email: 'Imeile', password: 'Lefoko la Lekunutu', signIn: 'Kena', signingIn: 'O kena...',
    noAccount: 'Ha o na akhaonto?', signUp: 'Ngodisa', needAccount: 'O hloka akhaonto?',
    registerHere: 'Ngodisa mona', customerLogin: 'Ho kena ha moreki',
    portalSubtitle: 'Porotale ya Ditsheko tsa Ditirelo', employeeSubtitle: 'Porotale ya Basebetsi',
    createAccount: 'Bopa Akhaonto', creatingAccount: 'E bopa Akhaonto...', joinPortal: 'Tsena Porotale ya Ditsheko',
    firstName: 'Lebitso', lastName: 'Sefane', phoneNumber: 'Nomoro ya Mohala',
    confirmPassword: 'Netefatsa Lefoko la Lekunutu', alreadyHaveAccount: 'O setse o na le akhaonto?',
    accountCreated: 'Akhaonto e Bopilwe!', accountCreatedMsg: 'Akhaonto ya hao e bopilwe ka katleho.',
    clickToLogin: 'Cofa mona ho ya sekerineng sa ho kena',
    transactions: 'Ditshebetso', disputes: 'Ditsheko', security: 'Tshireletso', logout: 'Tswa', dashboard: 'Phaposi',
    welcome: 'O amohelwa', accountNumber: 'Nomoro ya Akhaonto', quickActions: 'Diketsahalo tsa Potlako',
    viewTransactions: 'Sheba Ditshebetso', viewDisputes: 'Sheba Ditsheko',
    mfaEnabled: '✓ MFA e Kgontshitswe', mfaDisabled: '! MFA e Khutlisitswe', loading: 'E a laisa...', failedToLoad: 'Ho hlolehile ho laisa data',
    simulateTransactions: 'Etsa Ditshebetso', simulateDesc: 'E hlola ditshebetso tse 20 tsa ho leka',
    simulate: 'Etsa', simulating: 'E a etsa...', noTransactions: 'Ha ho ditshebetso',
    amount: 'Chelete', description: 'Tlhaloso', date: 'Letsatsi', status: 'Boemo', action: 'Ketso',
    disputeTransaction: 'Ngangisana le Tshebetso',
    myDisputes: 'Ditsheko Tsa Ka', noDisputes: 'Ha ho ditsheko', reason: 'Lebaka', summary: 'Kakaretso',
    reference: 'Nomoro ya Kgolagano', detailsOfDispute: 'Dintlha tsa Tsheko',
    disputeStep1Title: 'Ke ka lebaka la eng o ngangisana?', selectReason: 'Kgetha lebaka',
    next: 'E latelang', back: 'Morao', cancel: 'Khansela',
    disputeStep2Title: 'Fa dintlha tse ding', writeSummary: 'Ngola kakaretso',
    summaryPlaceholder: 'Hlalosa bothata ka botlalo...', submitDispute: 'Romela Tsheko', submitting: 'E a romela...',
    disputeSubmitted: 'Tsheko e Rometšwe!', disputeSubmittedMsg: 'Tsheko ya hao e rometšwe ka katleho.',
    close: 'Kwala',
    reasonUnauthorised: 'Tshebetso e sa Lumellwang', reasonIncorrectAmount: 'Chelete e Fosahetseng',
    reasonDoublePayment: 'Tefo e Etswa Habedi', reasonOther: 'Tse Ding',
    otherReasonPlaceholder: 'Hlalosa lebaka la hao...',
    securitySettings: 'Ditlhomelo tsa Tshireletso',
    profile: 'Profaele', editProfile: 'Fetola Profaele', updateProfile: 'Mpshafatsa Profaele',
    updating: 'E mpshafatsa...', profileUpdated: 'Profaele e mpshafatsitswe!',
    changePassword: 'Fetola Lefoko la Lekunutu', currentPassword: 'Lefoko la Lekunutu la Hona Joale',
    newPassword: 'Lefoko la Lekunutu le Lecha', confirmNewPassword: 'Netefatsa Lefoko la Lekunutu le Lecha',
    passwordChanged: 'Lefoko la Lekunutu le fetotswe!',
    saveChanges: 'Boloka Diphetogo', saving: 'E boloka...',
  },
  ts: {
    preferredLanguage: 'Ririmi ro Tsakela',
    email: 'I-imeyili', password: 'Phasiwedi', signIn: 'Nghena', signingIn: 'U nghena...',
    noAccount: 'Huna akhauntu?', signUp: 'Ngodhisa', needAccount: 'U lava akhauntu?',
    registerHere: 'Ngodhisa laha', customerLogin: 'Ku nghena ka mureki',
    portalSubtitle: 'Porotali ya Swiphikiso swa Timali', employeeSubtitle: 'Porotali ya Swirho',
    createAccount: 'Endla Akhauntu', creatingAccount: 'Yi endla Akhauntu...', joinPortal: 'Nghena Porotali ya Swiphikiso',
    firstName: 'Vito', lastName: 'Xivongo', phoneNumber: 'Nomboro ya Swihlawulekisi',
    confirmPassword: 'Tiyisa Phasiwedi', alreadyHaveAccount: 'U se na akhauntu?',
    accountCreated: 'Akhauntu Yi Endliwile!', accountCreatedMsg: 'Akhauntu ya wena yi endliwile hi ndlela yo tsakisa.',
    clickToLogin: 'Koma ndhawu yin\'wana ku ya eka skerini ya ku nghena',
    transactions: 'Swifaniso', disputes: 'Swiphikiso', security: 'Tshireletso', logout: 'Huma', dashboard: 'Bodo',
    welcome: 'U amukelekile', accountNumber: 'Nomboro ya Akhauntu', quickActions: 'Swendo Swa Xihatla',
    viewTransactions: 'Vona Swifaniso', viewDisputes: 'Vona Swiphikiso',
    mfaEnabled: '✓ MFA yi Pfuxeriwile', mfaDisabled: '! MFA yi Cimiwile', loading: 'Yi laya...', failedToLoad: 'Ku tsandzekile ku layisha data',
    simulateTransactions: 'Hlaya Swifaniso', simulateDesc: 'Yi endla swifaniso 20 swa ku linga',
    simulate: 'Hlaya', simulating: 'Yi hlaya...', noTransactions: 'Swifaniso a swi kona',
    amount: 'Xifunwa', description: 'Nhlamuselo', date: 'Siku', status: 'Xiyimo', action: 'Ntirho',
    disputeTransaction: 'Phikisa Xifaniso',
    myDisputes: 'Swiphikiso Swa Mina', noDisputes: 'Swiphikiso a swi kona', reason: 'Xivangelo', summary: 'Nkucetelo',
    reference: 'Nomboro ya Xikombiso', detailsOfDispute: 'Swilo swa Xiphikiso',
    disputeStep1Title: 'Xivangelo xa xini u phikisa?', selectReason: 'Hlawula xivangelo',
    next: 'Lexi landzelaka', back: 'Endzhaku', cancel: 'Khansela',
    disputeStep2Title: 'Nyika swilo swin\'wana', writeSummary: 'Tsala nkucetelo',
    summaryPlaceholder: 'Nhlamusela xiphikiso hi vulevu...', submitDispute: 'Rhuma Xiphikiso', submitting: 'Yi rhuma...',
    disputeSubmitted: 'Xiphikiso Xi Rhumiwile!', disputeSubmittedMsg: 'Xiphikiso xa wena xi rhumiwile hi ndlela yo tsakisa.',
    close: 'Pfala',
    reasonUnauthorised: 'Xifaniso Lexi nga Pfumeleriwa', reasonIncorrectAmount: 'Xifunwa Lexi nga Lulami',
    reasonDoublePayment: 'Nkhensa wu Endliwile Kaviri', reasonOther: 'Swin\'wana',
    otherReasonPlaceholder: 'Nhlamusela xivangelo xa wena...',
    securitySettings: 'Swilungiselelo swa Tshireletso',
    profile: 'Profaele', editProfile: 'Cinca Profaele', updateProfile: 'Hlayisa Profaele',
    updating: 'Yi hlayisa...', profileUpdated: 'Profaele yi hlayisiwile!',
    changePassword: 'Cinca Phasiwedi', currentPassword: 'Phasiwedi ya Sweswi',
    newPassword: 'Phasiwedi Leyintshwa', confirmNewPassword: 'Tiyisa Phasiwedi Leyintshwa',
    passwordChanged: 'Phasiwedi yi cinciwile!',
    saveChanges: 'Hlayisa Swincinci', saving: 'Yi hlayisa...',
  },
  ss: {
    preferredLanguage: 'Lulwimi Lolutandvekile',
    email: 'I-imeyili', password: 'Liphasiwedi', signIn: 'Ngena', signingIn: 'Uyangena...',
    noAccount: 'Awunayo i-akhawunti?', signUp: 'Bhalisa', needAccount: 'Udvinga i-akhawunti?',
    registerHere: 'Bhalisa lapha', customerLogin: 'Kungena kwemtengisi',
    portalSubtitle: 'Indzawo Yetikhalo Temali', employeeSubtitle: 'Indzawo Yabasebenzi',
    createAccount: 'Dala I-akhawunti', creatingAccount: 'Idala I-akhawunti...', joinPortal: 'Joyina Indzawo Yetikhalo',
    firstName: 'Libito', lastName: 'Sibongo', phoneNumber: 'Inombolo Yefoni',
    confirmPassword: 'Qinisekisa Liphasiwedi', alreadyHaveAccount: 'Sewunayo i-akhawunti?',
    accountCreated: 'I-akhawunti Idaliwile!', accountCreatedMsg: 'I-akhawunti yakho idaliwile ngemphumelelo.',
    clickToLogin: 'Chofoza noma kuphi ukuya esikrini sokungena',
    transactions: 'Tikhokhelwano', disputes: 'Tikhalo', security: 'Kuvikeleka', logout: 'Phuma', dashboard: 'Ibhodi',
    welcome: 'Wamukelekile', accountNumber: 'Inombolo ye-akhawunti', quickActions: 'Tintfo Letisheshako',
    viewTransactions: 'Buka Tikhokhelwano', viewDisputes: 'Buka Tikhalo',
    mfaEnabled: '✓ MFA Ivuliwe', mfaDisabled: '! MFA Ivaliwile', loading: 'Iyalayisha...', failedToLoad: 'Kuhlulekile kulayisha idatha',
    simulateTransactions: 'Lingisa Tikhokhelwano', simulateDesc: 'Ikhiqita tikhokhelwano letingema-20 tekuhlola',
    simulate: 'Lingisa', simulating: 'Iyalingisa...', noTransactions: 'Atikho tikhokhelwano',
    amount: 'Inani', description: 'Incazelo', date: 'Lusuku', status: 'Simo', action: 'Intfo',
    disputeTransaction: 'Phikisa Inkhokhelwano',
    myDisputes: 'Tikhalo Tami', noDisputes: 'Atikho tikhalo', reason: 'Sizatfu', summary: 'Sifinyeto',
    reference: 'Inombolo Yokubhekisa', detailsOfDispute: 'Imininingwane Yekhalo',
    disputeStep1Title: 'Unikani uphikisa?', selectReason: 'Khetha sizatfu',
    next: 'Lokulandzelako', back: 'Emuva', cancel: 'Khansela',
    disputeStep2Title: 'Niketa imininingwane lengetiwe', writeSummary: 'Bhala sifinyeto',
    summaryPlaceholder: 'Chaza indzaba ngokunzulu...', submitDispute: 'Thumela Sikhalo', submitting: 'Iyathumela...',
    disputeSubmitted: 'Sikhalo Sithunyiwe!', disputeSubmittedMsg: 'Sikhalo sakho sithunyiwe ngemphumelelo.',
    close: 'Vala',
    reasonUnauthorised: 'Inkhokhelwano Lengagunyaziswanga', reasonIncorrectAmount: 'Inani Lelingalungile',
    reasonDoublePayment: 'Inkhokhelwano Ephindwe Kabili', reasonOther: 'Okunye',
    otherReasonPlaceholder: 'Chaza sizatfu sakho...',
    securitySettings: 'Tilungiselelo Tekuvikeleka',
    profile: 'Iphrofayili', editProfile: 'Hlela Iphrofayili', updateProfile: 'Buyekeza Iphrofayili',
    updating: 'Iyabuyekeza...', profileUpdated: 'Iphrofayili ibuyekezwe!',
    changePassword: 'Shintsha Liphasiwedi', currentPassword: 'Liphasiwedi Lanjalo',
    newPassword: 'Liphasiwedi Lelitsha', confirmNewPassword: 'Qinisekisa Liphasiwedi Lelitsha',
    passwordChanged: 'Liphasiwedi lishintshiwe!',
    saveChanges: 'Gcina Tinguquko', saving: 'Igcina...',
  },
  ve: {
    preferredLanguage: 'Luambo lwo Ṱalutshedza',
    email: 'Imeili', password: 'Phasiwede', signIn: 'Dzhena', signingIn: 'U a dzhena...',
    noAccount: 'Huna akhauntu?', signUp: 'Ŋwalisei', needAccount: 'U ṱoḓa akhauntu?',
    registerHere: 'Ŋwalisei afha', customerLogin: 'U dzhena ha mutengi',
    portalSubtitle: 'Portali ya Mburedo ya Tshelede', employeeSubtitle: 'Portali ya Vhashumi',
    createAccount: 'Bveledza Akhauntu', creatingAccount: 'I bveledza Akhauntu...', joinPortal: 'Dzhena Portali ya Mburedo',
    firstName: 'Dzina', lastName: 'Pfuko', phoneNumber: 'Nomboro ya Foni',
    confirmPassword: 'Khwaṱhisedza Phasiwede', alreadyHaveAccount: 'U na akhauntu?',
    accountCreated: 'Akhauntu yo Bveledzwa!', accountCreatedMsg: 'Akhauntu yau yo bveledzwa nga ndila yo takudzaho.',
    clickToLogin: 'Kanda fhasi musi u tshi ya kha skerini ya u dzhena',
    transactions: 'Zwitirathiwa', disputes: 'Mburedo', security: 'Vhudzulaganyi', logout: 'Bva', dashboard: 'Bodo',
    welcome: 'U amukelwa', accountNumber: 'Nomboro ya Akhauntu', quickActions: 'Mishumo ya U Itea Nga U Bvela',
    viewTransactions: 'Vhona Zwitirathiwa', viewDisputes: 'Vhona Mburedo',
    mfaEnabled: '✓ MFA yo Pfuriswa', mfaDisabled: '! MFA yo Dzimwa', loading: 'I a layisha...', failedToLoad: 'Ho kundwa u layisha data',
    simulateTransactions: 'Shumisa Zwitirathiwa', simulateDesc: 'I bveledza zwitirathiwa 20 zwa u lingedza',
    simulate: 'Shumisa', simulating: 'I a shumisa...', noTransactions: 'A huna zwitirathiwa',
    amount: 'Mbilaelo', description: 'Ṱalutshedzo', date: 'Datumu', status: 'Maimo', action: 'Mushumo',
    disputeTransaction: 'Hanedza Zwitirathiwa',
    myDisputes: 'Mburedo Yanga', noDisputes: 'A huna mburedo', reason: 'Tshikhala', summary: 'Pfufhi',
    reference: 'Nomboro ya Khoromelo', detailsOfDispute: 'Vhukovhekanywa ha Mburedo',
    disputeStep1Title: 'Ndi ngani u a hanedza?', selectReason: 'Nanga tshikhala',
    next: 'Tshi tevhelaho', back: 'Murahu', cancel: 'Khansela',
    disputeStep2Title: 'Ṋea vhukovhekanywa vhunzhi', writeSummary: 'Ṅwala pfufhi',
    summaryPlaceholder: 'Ṱalutshedza vhudanedzo nga vhuḓalo...', submitDispute: 'Rumela Mburedo', submitting: 'I a rumela...',
    disputeSubmitted: 'Mburedo yo Rumelwa!', disputeSubmittedMsg: 'Mburedo yau yo rumelwa nga ndila yo takudzaho.',
    close: 'Vhala',
    reasonUnauthorised: 'Zwitirathiwa zwo sa Tendwaho', reasonIncorrectAmount: 'Mbilaelo i sa Lulami',
    reasonDoublePayment: 'Mbadelo yo Itwa Kavhili', reasonOther: 'Zwinzhi',
    otherReasonPlaceholder: 'Ṱalutshedza tshikhala tshaụ...',
    securitySettings: 'Maitele a Vhudzulaganyi',
    profile: 'Profhaele', editProfile: 'Dzudzanya Profhaele', updateProfile: 'Fhedzisa Profhaele',
    updating: 'I fhedzisa...', profileUpdated: 'Profhaele yo fhedziswa!',
    changePassword: 'Shandukisa Phasiwede', currentPassword: 'Phasiwede ya Zwino',
    newPassword: 'Phasiwede Ntswa', confirmNewPassword: 'Khwaṱhisedza Phasiwede Ntswa',
    passwordChanged: 'Phasiwede yo shandukiswa!',
    saveChanges: 'Vhulunga Mbudziso', saving: 'I vhulunga...',
  },
  nr: {
    preferredLanguage: 'Ulimi Oluthandekako',
    email: 'I-imeyili', password: 'Iphasiwedi', signIn: 'Ngena', signingIn: 'Uyangena...',
    noAccount: 'Awunayo i-akhawunti?', signUp: 'Bhalisa', needAccount: 'Ufuna i-akhawunti?',
    registerHere: 'Bhalisa lapha', customerLogin: 'Ukungena kwabathengi',
    portalSubtitle: 'Ikhuliso Lezinkinga Zemali', employeeSubtitle: 'Ikhuliso Labasebenzi',
    createAccount: 'Dala I-akhawunti', creatingAccount: 'Idala I-akhawunti...', joinPortal: 'Joyina Ikhuliso Lezinkinga',
    firstName: 'Ibizo', lastName: 'Isibongo', phoneNumber: 'Inombolo Yefoni',
    confirmPassword: 'Qinisekisa Iphasiwedi', alreadyHaveAccount: 'Sewunayo i-akhawunti?',
    accountCreated: 'I-akhawunti Idaliwile!', accountCreatedMsg: 'I-akhawunti yakho idaliwile ngemphumelelo.',
    clickToLogin: 'Chofoza noma kuphi ukuya esikrini sokungena',
    transactions: 'Iimali', disputes: 'Iinkinga', security: 'Ukuphepha', logout: 'Phuma', dashboard: 'Ibhodi',
    welcome: 'Wamukelekile', accountNumber: 'Inombolo ye-akhawunti', quickActions: 'Izenzo Ezisheshako',
    viewTransactions: 'Buka Iimali', viewDisputes: 'Buka Iinkinga',
    mfaEnabled: '✓ MFA Ivuliwe', mfaDisabled: '! MFA Ivaliwile', loading: 'Iyalayisha...', failedToLoad: 'Yehlulekile ukulayisha idatha',
    simulateTransactions: 'Lingisa Iimali', simulateDesc: 'Ikhiqiza iimali eziyi-20 zokuhlola',
    simulate: 'Lingisa', simulating: 'Iyalingisa...', noTransactions: 'Azikho iimali',
    amount: 'Inani', description: 'Incazelo', date: 'Usuku', status: 'Isimo', action: 'Isenzo',
    disputeTransaction: 'Phikisa Imali',
    myDisputes: 'Iinkinga Zami', noDisputes: 'Azikho iinkinga', reason: 'Isizathu', summary: 'Isifinyezo',
    reference: 'Inombolo Yokubhekisa', detailsOfDispute: 'Imininingwane Yeenkinga',
    disputeStep1Title: 'Kungani uphikisa?', selectReason: 'Khetha isizathu',
    next: 'Okulandelako', back: 'Emuva', cancel: 'Khansela',
    disputeStep2Title: 'Nikeza imininingwane engaphezulu', writeSummary: 'Bhala isifinyezo',
    summaryPlaceholder: 'Chaza inkinga ngokuningiliziwe...', submitDispute: 'Thumela Isikhalazo', submitting: 'Iyathumela...',
    disputeSubmitted: 'Isikhalazo Sithunyiwe!', disputeSubmittedMsg: 'Isikhalazo sakho sithunyiwe ngemphumelelo.',
    close: 'Vala',
    reasonUnauthorised: 'Imali Engagunyaziswanga', reasonIncorrectAmount: 'Inani Elingalungile',
    reasonDoublePayment: 'Imali Ephindwe Kabili', reasonOther: 'Okunye',
    otherReasonPlaceholder: 'Chaza isizathu sakho...',
    securitySettings: 'Izilungiselelo Zokuphepha',
    profile: 'Iphrofayili', editProfile: 'Hlela Iphrofayili', updateProfile: 'Buyekeza Iphrofayili',
    updating: 'Iyabuyekeza...', profileUpdated: 'Iphrofayili ibuyekezwe!',
    changePassword: 'Shintsha Iphasiwedi', currentPassword: 'Iphasiwedi Yangamanje',
    newPassword: 'Iphasiwedi Entsha', confirmNewPassword: 'Qinisekisa Iphasiwedi Entsha',
    passwordChanged: 'Iphasiwedi ishintshiwe!',
    saveChanges: 'Gcina Izinguquko', saving: 'Igcina...',
  },
}

export function getTranslations(code: string): Translations {
  return t[code] ?? t['en']
}
