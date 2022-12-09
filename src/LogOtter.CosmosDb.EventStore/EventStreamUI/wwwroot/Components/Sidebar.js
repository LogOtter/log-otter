export default {
    data() {
        return {currentHash: window.location.hash}
    },
    props: {
      eventStreams: Array  
    },
    computed: {
        currentPath() {
            return this.currentHash.slice(1) || '/';
        },
    },
    mounted() {
        window.addEventListener('hashchange', () => {
            this.currentHash = window.location.hash
        });
    },
    template: `
    <div class="d-flex flex-column flex-shrink-0 p-3 text-bg-dark" style="width: 280px">
        <a href="/" class="d-flex align-items-center mb-3 mb-md-0 me-md-auto text-white text-decoration-none">
            <img src="Resources/log-otter-grayscale.svg" class="me-2" width="32" alt="LogOtter" />
            <span class="fs-5">Event Stream Viewer</span>
        </a>
        <hr>
        <ul class="nav nav-pills flex-column mb-auto">
            <li>
                <a href="#" class="nav-link text-white" :class="{active: this.currentPath == '/'}">
                    <i class="bi-house-door me-2"></i>
                    Home
                </a>
            </li>
            <li v-for="eventStream in eventStreams" :key="eventStream.name">
                <a :href="'#/' + eventStream.name" class="nav-link text-white" :class="{active: this.currentPath === '/' + eventStream.name || this.currentPath.startsWith('/' + eventStream.name + '/')}">
                    <i class="bi-journals me-2"></i>
                    {{ eventStream.name }}
                </a>
            </li>
        </ul>
    </div>
    `
}